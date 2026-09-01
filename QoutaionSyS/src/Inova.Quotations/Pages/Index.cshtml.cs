using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages;

public class IndexModel(QuotationDbContext db) : PageModel
{
    public List<Quotation> Quotations { get; private set; } = [];

    /// <summary>Nesting level per quotation id: 0 for an original, 1+ for a sub-quotation.</summary>
    public Dictionary<int, int> Depth { get; private set; } = [];

    /// <summary>How many direct sub-quotations each quotation has.</summary>
    public Dictionary<int, int> RevisionCount { get; private set; } = [];

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

        RevisionCount = all
            .Where(x => x.ParentId is not null)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // A filtered result is a flat list — nesting a partial set would be misleading.
        if (!string.IsNullOrWhiteSpace(Q))
        {
            Quotations = all;
            Depth = all.ToDictionary(x => x.Id, _ => 0);
            return;
        }

        var children = all
            .Where(x => x.ParentId is not null)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.JobNo, StringComparer.Ordinal).ToList());

        var ordered = new List<Quotation>(all.Count);
        var depth = new Dictionary<int, int>(all.Count);

        void Append(Quotation quotation, int level)
        {
            ordered.Add(quotation);
            depth[quotation.Id] = level;

            if (!children.TryGetValue(quotation.Id, out var revisions)) return;
            foreach (var revision in revisions) Append(revision, level + 1);
        }

        // Orphans (parent deleted, ParentId nulled) fall through as roots on their own.
        var known = all.Select(x => x.Id).ToHashSet();
        foreach (var root in all.Where(x => x.ParentId is null || !known.Contains(x.ParentId.Value)))
            Append(root, 0);

        Quotations = ordered;
        Depth = depth;
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
