using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages.PurchaseOrders;

public class PrintModel(QuotationDbContext db) : PageModel
{
    public PurchaseOrder PurchaseOrder { get; private set; } = null!;
    public CompanyProfile Company { get; private set; } = null!;

    /// <summary>Hides the on-screen toolbar so it never reaches the PDF renderer.</summary>
    [BindProperty(SupportsGet = true)]
    public bool Bare { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var order = await db.PurchaseOrders
            .Include(p => p.Items.OrderBy(i => i.SortOrder))
            .Include(p => p.Quotation)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order is null) return NotFound();

        PurchaseOrder = order;
        Company = await db.CompanyProfiles.AsNoTracking().FirstAsync();
        return Page();
    }
}
