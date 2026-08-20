using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages.Quotations;

public class EditModel(QuotationDbContext db, IWebHostEnvironment env) : PageModel
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxImageBytes = 8 * 1024 * 1024;

    [BindProperty] public Quotation Input { get; set; } = new();

    /// <summary>Caption edits and delete flags for images already attached.</summary>
    [BindProperty] public List<ExistingImage> ExistingImages { get; set; } = [];

    [BindProperty] public List<IFormFile> NewImages { get; set; } = [];

    public List<QuotationImage> Images { get; private set; } = [];
    public bool IsNew => Input.Id == 0;

    [TempData] public string? Flash { get; set; }

    public class ExistingImage
    {
        public int Id { get; set; }
        public string? Caption { get; set; }
        public bool Delete { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            var profile = await db.CompanyProfiles.AsNoTracking().FirstAsync();
            Input = new Quotation
            {
                QuoteDate = DateOnly.FromDateTime(DateTime.Today),
                VatPercent = profile.DefaultVatPercent,
                Terms = profile.DefaultTerms,
                JobNo = await NextJobNoAsync(),
                Items = [new QuotationItem { SortOrder = 0, Quantity = 1 }]
            };
            return Page();
        }

        var quotation = await db.Quotations
            .Include(q => q.Items.OrderBy(i => i.SortOrder))
            .Include(q => q.Images.OrderBy(i => i.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation is null) return NotFound();

        Input = quotation;
        Images = quotation.Images;
        ExistingImages = quotation.Images
            .Select(i => new ExistingImage { Id = i.Id, Caption = i.Caption })
            .ToList();

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

        var jobNoTaken = await db.Quotations
            .AnyAsync(q => q.JobNo == Input.JobNo && q.Id != Input.Id);
        if (jobNoTaken)
            ModelState.AddModelError("Input.JobNo", "That job number is already used by another quotation.");

        foreach (var file in NewImages.Where(f => f.Length > 0))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
                ModelState.AddModelError("NewImages", $"{file.FileName}: only JPG, PNG, WEBP and GIF images are accepted.");
            else if (file.Length > MaxImageBytes)
                ModelState.AddModelError("NewImages", $"{file.FileName}: images must be 8 MB or smaller.");
        }

        if (!ModelState.IsValid)
        {
            await ReloadImagesAsync();
            return Page();
        }

        Quotation target;

        if (Input.Id == 0)
        {
            target = new Quotation { CreatedAt = DateTime.UtcNow };
            db.Quotations.Add(target);
        }
        else
        {
            var found = await db.Quotations
                .Include(q => q.Items)
                .Include(q => q.Images)
                .FirstOrDefaultAsync(q => q.Id == Input.Id);
            if (found is null) return NotFound();
            target = found;
        }

        target.JobNo = Input.JobNo.Trim();
        target.SubjectEn = Input.SubjectEn.Trim();
        target.SubjectAr = Blank(Input.SubjectAr);
        target.QuoteDate = Input.QuoteDate;
        target.CompanyEn = Input.CompanyEn.Trim();
        target.CompanyAr = Blank(Input.CompanyAr);
        target.ContactPersonEn = Input.ContactPersonEn.Trim();
        target.ContactPersonAr = Blank(Input.ContactPersonAr);
        target.ContactDetails = Blank(Input.ContactDetails);
        target.VatPercent = Input.VatPercent;
        target.Terms = Input.Terms.Trim();
        target.NoteEn = Blank(Input.NoteEn);
        target.NoteAr = Blank(Input.NoteAr);
        target.UpdatedAt = DateTime.UtcNow;

        // Line items are small and order-sensitive: replace them wholesale.
        target.Items.Clear();
        foreach (var item in Input.Items)
        {
            target.Items.Add(new QuotationItem
            {
                SortOrder = item.SortOrder,
                DescriptionEn = item.DescriptionEn.Trim(),
                DescriptionAr = Blank(item.DescriptionAr),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        // Caption edits and removals on images already attached.
        foreach (var edit in ExistingImages)
        {
            var image = target.Images.FirstOrDefault(i => i.Id == edit.Id);
            if (image is null) continue;

            if (edit.Delete)
            {
                DeleteFile(image.Path);
                target.Images.Remove(image);
            }
            else
            {
                image.Caption = Blank(edit.Caption);
            }
        }

        var nextSort = target.Images.Count == 0 ? 0 : target.Images.Max(i => i.SortOrder) + 1;
        foreach (var file in NewImages.Where(f => f.Length > 0))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var name = $"{Guid.NewGuid():N}{ext}";
            var uploads = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            await using (var stream = System.IO.File.Create(Path.Combine(uploads, name)))
                await file.CopyToAsync(stream);

            target.Images.Add(new QuotationImage
            {
                Path = $"/uploads/{name}",
                Caption = Path.GetFileNameWithoutExtension(file.FileName),
                SortOrder = nextSort++
            });
        }

        await db.SaveChangesAsync();

        Flash = $"Quotation {target.JobNo} saved.";
        return RedirectToPage("/Quotations/Edit", new { id = target.Id });
    }

    private async Task ReloadImagesAsync()
    {
        Images = Input.Id == 0
            ? []
            : await db.QuotationImages
                .Where(i => i.QuotationId == Input.Id)
                .OrderBy(i => i.SortOrder)
                .AsNoTracking()
                .ToListAsync();
    }

    /// <summary>Suggests the next sequential job number, e.g. INV-2026-0007.</summary>
    private async Task<string> NextJobNoAsync()
    {
        var year = DateTime.Today.Year;
        var prefix = $"INV-{year}-";

        var last = await db.Quotations
            .Where(q => q.JobNo.StartsWith(prefix))
            .OrderByDescending(q => q.JobNo)
            .Select(q => q.JobNo)
            .FirstOrDefaultAsync();

        var next = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var parsed))
            next = parsed + 1;

        return $"{prefix}{next:D4}";
    }

    private void DeleteFile(string webPath)
    {
        try
        {
            var full = Path.Combine(env.WebRootPath, webPath.TrimStart('/'));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
        catch (IOException)
        {
            // A leftover file on disk is not worth failing the save over.
        }
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
