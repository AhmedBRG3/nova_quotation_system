using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages.PurchaseOrders;

public class IndexModel(QuotationDbContext db) : PageModel
{
    public List<PurchaseOrder> PurchaseOrders { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [TempData] public string? Flash { get; set; }

    public async Task OnGetAsync()
    {
        var query = db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Quotation)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = $"%{Q.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.PoNo, term) ||
                EF.Functions.ILike(p.SubjectEn, term) ||
                EF.Functions.ILike(p.VendorNameEn, term));
        }

        PurchaseOrders = await query
            .OrderByDescending(p => p.PoDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var order = await db.PurchaseOrders.FindAsync(id);
        if (order is not null)
        {
            db.PurchaseOrders.Remove(order);
            await db.SaveChangesAsync();
            Flash = $"Purchase order {order.PoNo} deleted.";
        }
        return RedirectToPage();
    }
}
