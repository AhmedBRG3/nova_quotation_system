using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages.Quotations;

public class PrintModel(QuotationDbContext db, IConfiguration config) : PageModel
{
    public Quotation Quotation { get; private set; } = null!;
    public CompanyProfile Company { get; private set; } = null!;

    /// <summary>Hides the on-screen toolbar so it never reaches the PDF renderer.</summary>
    [BindProperty(SupportsGet = true)]
    public bool Bare { get; set; }

    /// <summary>
    /// Scheme and host the image links in the PDF are stamped with, e.g. "https://quotes.example.com".
    /// They must be absolute: a relative href would resolve against the file:// location the reader
    /// opened the download from, and break.
    ///
    /// Prefers the PublicBaseUrl setting over the request's own host, because the request that
    /// renders a PDF is usually the local one — headless Chrome fetching this page over
    /// localhost, or you opening it on the machine the app runs on. Stamping that host would
    /// bake "localhost" into a document meant to be read on someone else's phone or laptop,
    /// where it resolves to their device and dead-ends.
    /// </summary>
    public string BaseUrl { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var quotation = await db.Quotations
            .Include(q => q.Items.OrderBy(i => i.SortOrder))
            .Include(q => q.Images.OrderBy(i => i.SortOrder))
            .Include(q => q.Parent)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation is null) return NotFound();

        Quotation = quotation;
        Company = await db.CompanyProfiles.AsNoTracking().FirstAsync();
        BaseUrl = (config["PublicBaseUrl"] ?? "").TrimEnd('/') is { Length: > 0 } configured
            ? configured
            : $"{Request.Scheme}://{Request.Host}";
        return Page();
    }
}
