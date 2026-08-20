using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages;

public class IndexModel(QuotationDbContext db) : PageModel
{
    public List<Quotation> Quotations { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [TempData] public string? Flash { get; set; }

    public async Task OnGetAsync()
    {
        var query = db.Quotations.Include(x => x.Items).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = $"%{Q.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.JobNo, term) ||
                EF.Functions.ILike(x.SubjectEn, term) ||
                EF.Functions.ILike(x.CompanyEn, term));
        }

        Quotations = await query
            .OrderByDescending(x => x.QuoteDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync();
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
