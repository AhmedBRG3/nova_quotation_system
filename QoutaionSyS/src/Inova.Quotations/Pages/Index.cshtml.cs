using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages;

public class IndexModel(QuotationDbContext db) : PageModel
{
    /// <summary>Rows for the table: originals only, never sub-quotations.</summary>
    public List<Quotation> Quotations { get; private set; } = [];

    /// <summary>Sub-quotations keyed by the original they belong to — the only place revisions surface.</summary>
    public Dictionary<int, List<Quotation>> RevisionsByParent { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [TempData] public string? Flash { get; set; }

    public async Task OnGetAsync()
    {
        var query = db.Quotations.Include(x => x.Items).Include(x => x.Parent).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = $"%{Q.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.JobNo, term) ||
                EF.Functions.ILike(x.SubjectEn, term) ||
                EF.Functions.ILike(x.CompanyEn, term));
        }

        var all = await query
            .OrderByDescending(x => x.QuoteDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        RevisionsByParent = all
            .Where(x => x.ParentId is not null)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.JobNo, StringComparer.Ordinal).ToList());

        // A filtered result is a flat list: a revision that matches the search has to be
        // reachable, and it has no row of its own to hang off when its original is filtered out.
        if (!string.IsNullOrWhiteSpace(Q))
        {
            Quotations = all;
            return;
        }

        // Only originals get a row. Revisions live in the per-row dropdown instead.
        // Orphans (parent deleted, ParentId nulled by the FK) stand as originals in their own right.
        var known = all.Select(x => x.Id).ToHashSet();
        Quotations = all
            .Where(x => x.ParentId is null || !known.Contains(x.ParentId.Value))
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var quotation = await db.Quotations.FindAsync(id);
        if (quotation is not null)
        {
            db.Quotations.Remove(quotation);
            await db.SaveChangesAsync();
            Flash = $"Quotation {quotation.JobNo} deleted.";
        }
        return RedirectToPage();
    }
}
