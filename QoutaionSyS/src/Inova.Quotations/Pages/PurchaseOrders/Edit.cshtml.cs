using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages.PurchaseOrders;

public class EditModel(QuotationDbContext db) : PageModel
{
    [BindProperty] public PurchaseOrder Input { get; set; } = new();

    public bool IsNew => Input.Id == 0;

    /// <summary>Job number of the linked quotation, shown in the banner and the header line.</summary>
    public string? QuotationJobNo { get; private set; }

    [TempData] public string? Flash { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id, int? fromQuotation)
    {
        // "Create PO" from a quotation: carry the scope across, but not the client's prices —
        // what the vendor charges is a different number and has to be entered.
        if (fromQuotation is int quotationId)
        {
            var quotation = await db.Quotations
                .Include(q => q.Items.OrderBy(i => i.SortOrder))
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == quotationId);

            if (quotation is null) return NotFound();

            var profile = await db.CompanyProfiles.AsNoTracking().FirstAsync();

            Input = new PurchaseOrder
            {
                PoNo = await NextPoNoAsync(),
                PoDate = DateOnly.FromDateTime(DateTime.Today),
                QuotationId = quotation.Id,
                SubjectEn = quotation.SubjectEn,
                SubjectAr = quotation.SubjectAr,
                VatPercent = profile.DefaultVatPercent,
                Terms = profile.DefaultPoTerms,
                Items = quotation.Items
                    .Select(i => new PurchaseOrderItem
                    {
                        SortOrder = i.SortOrder,
                        DescriptionEn = i.DescriptionEn,
                        DescriptionAr = i.DescriptionAr,
                        Quantity = i.Quantity,
                        UnitPrice = 0m
                    })
                    .ToList()
            };

            if (Input.Items.Count == 0)
                Input.Items.Add(new PurchaseOrderItem { SortOrder = 0, Quantity = 1 });

            QuotationJobNo = quotation.JobNo;
            return Page();
        }

        if (id is null)
        {
            var profile = await db.CompanyProfiles.AsNoTracking().FirstAsync();
            Input = new PurchaseOrder
            {
                PoNo = await NextPoNoAsync(),
                PoDate = DateOnly.FromDateTime(DateTime.Today),
                VatPercent = profile.DefaultVatPercent,
                Terms = profile.DefaultPoTerms,
                Items = [new PurchaseOrderItem { SortOrder = 0, Quantity = 1 }]
            };
            return Page();
        }

        var order = await db.PurchaseOrders
            .Include(p => p.Items.OrderBy(i => i.SortOrder))
            .Include(p => p.Quotation)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order is null) return NotFound();

        Input = order;
        QuotationJobNo = order.Quotation?.JobNo;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Drop rows the user left completely blank before validating.
        Input.Items = Input.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.DescriptionEn)
                        || !string.IsNullOrWhiteSpace(i.DescriptionAr)
                        || i.UnitPrice != 0)
            .ToList();

        // Those blank rows may have left validation errors behind.
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Input.Items[")).ToList())
            ModelState.Remove(key);

        for (var i = 0; i < Input.Items.Count; i++)
        {
            var item = Input.Items[i];
            item.SortOrder = i;
            if (string.IsNullOrWhiteSpace(item.DescriptionEn))
                ModelState.AddModelError($"Input.Items[{i}].DescriptionEn", "Description is required");
            if (item.Quantity < 0)
                ModelState.AddModelError($"Input.Items[{i}].Quantity", "Quantity must be 0 or more");
            if (item.UnitPrice < 0)
                ModelState.AddModelError($"Input.Items[{i}].UnitPrice", "Unit price must be 0 or more");
        }

        if (Input.Items.Count == 0)
            ModelState.AddModelError("Input.Items", "Add at least one line item.");

        var poNoTaken = await db.PurchaseOrders
            .AnyAsync(p => p.PoNo == Input.PoNo && p.Id != Input.Id);
        if (poNoTaken)
            ModelState.AddModelError("Input.PoNo", "That PO number is already used by another purchase order.");

        if (!ModelState.IsValid)
        {
            await ReloadQuotationJobNoAsync();
            return Page();
        }

        PurchaseOrder target;

        if (Input.Id == 0)
        {
            target = new PurchaseOrder { CreatedAt = DateTime.UtcNow };
            db.PurchaseOrders.Add(target);
        }
        else
        {
            var found = await db.PurchaseOrders
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == Input.Id);
            if (found is null) return NotFound();
            target = found;
        }

        target.PoNo = Input.PoNo.Trim();
        target.PoDate = Input.PoDate;
        target.QuotationId = Input.QuotationId;
        target.SubjectEn = Input.SubjectEn.Trim();
        target.SubjectAr = Blank(Input.SubjectAr);
        target.VendorNameEn = Input.VendorNameEn.Trim();
        target.VendorNameAr = Blank(Input.VendorNameAr);
        target.VendorContactPersonEn = Blank(Input.VendorContactPersonEn);
        target.VendorContactPersonAr = Blank(Input.VendorContactPersonAr);
        target.VendorContactDetails = Blank(Input.VendorContactDetails);
        target.VendorTaxNumber = Blank(Input.VendorTaxNumber);
        target.VatPercent = Input.VatPercent;
        target.Terms = Input.Terms.Trim();
        target.NoteEn = Blank(Input.NoteEn);
        target.NoteAr = Blank(Input.NoteAr);
        target.UpdatedAt = DateTime.UtcNow;

        // Line items are small and order-sensitive: replace them wholesale.
        target.Items.Clear();
        foreach (var item in Input.Items)
        {
            target.Items.Add(new PurchaseOrderItem
            {
                SortOrder = item.SortOrder,
                DescriptionEn = item.DescriptionEn.Trim(),
                DescriptionAr = Blank(item.DescriptionAr),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        await db.SaveChangesAsync();

        Flash = $"Purchase order {target.PoNo} saved.";
        return RedirectToPage("/PurchaseOrders/Edit", new { id = target.Id });
    }

    private async Task ReloadQuotationJobNoAsync()
    {
        if (Input.QuotationId is not int quotationId) return;

        QuotationJobNo = await db.Quotations
            .Where(q => q.Id == quotationId)
            .Select(q => q.JobNo)
            .FirstOrDefaultAsync();
    }

    /// <summary>Suggests the next sequential PO number, e.g. PO-2026-0007.</summary>
    private async Task<string> NextPoNoAsync()
    {
        var year = DateTime.Today.Year;
        var prefix = $"PO-{year}-";

        var numbers = await db.PurchaseOrders
            .Where(p => p.PoNo.StartsWith(prefix))
            .Select(p => p.PoNo)
            .ToListAsync();

        var next = numbers
            .Select(p => p[prefix.Length..])
            .Select(suffix => int.TryParse(suffix, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{next:D4}";
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
