using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages.Quotations;

public class PrintModel(QuotationDbContext db) : PageModel
{
    public Quotation Quotation { get; private set; } = null!;
    public CompanyProfile Company { get; private set; } = null!;

    /// <summary>Hides the on-screen toolbar so it never reaches the PDF renderer.</summary>
    [BindProperty(SupportsGet = true)]
    public bool Bare { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var quotation = await db.Quotations
            .Include(q => q.Items.OrderBy(i => i.SortOrder))
            .Include(q => q.Images.OrderBy(i => i.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation is null) return NotFound();

        Quotation = quotation;
        Company = await db.CompanyProfiles.AsNoTracking().FirstAsync();
        return Page();
    }
}
