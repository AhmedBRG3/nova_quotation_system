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

    /// <summary>Id of the quotation this one is being derived from, while a sub-quotation is unsaved.</summary>
    [BindProperty] public int? ReviseFromId { get; set; }

    public bool IsRevision => ReviseFromId.HasValue;

    /// <summary>Job number of the original this revision hangs off, shown in the banner.</summary>
    public string? ParentJobNo { get; private set; }

    /// <summary>Job number the content was copied from, when that is not the original itself.</summary>
    public string? CopiedFromJobNo { get; private set; }

    [TempData] public string? Flash { get; set; }

    public class ExistingImage
    {
        public int Id { get; set; }
        public string? Caption { get; set; }
        public bool Delete { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id, int? reviseFrom)
    {
        // "Revise" from the list: load the original, then present it as an unsaved copy.
        if (reviseFrom is int parentId)
        {
            var parent = await db.Quotations
                .Include(q => q.Items.OrderBy(i => i.SortOrder))
                .Include(q => q.Images.OrderBy(i => i.SortOrder))
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == parentId);

            if (parent is null) return NotFound();

            var rootId = await RootIdAsync(parentId);

            Input = parent;
            Input.Id = 0;                                        // force an insert on save
            Input.JobNo = await NextRevisionJobNoAsync(parentId);
            ReviseFromId = parentId;                             // content comes from here
            ParentJobNo = await JobNoAsync(rootId);              // but it belongs to the original
            CopiedFromJobNo = rootId == parentId ? null : parent.JobNo;

            Images = parent.Images;
            ExistingImages = parent.Images
                .Select(i => new ExistingImage { Id = i.Id, Caption = i.Caption })
                .ToList();

            return Page();
        }

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

    public Task<IActionResult> OnPostAsync() => SaveAsync();

    /// <summary>Saves the edited form as a brand-new sub-quotation instead of overwriting.</summary>
    public async Task<IActionResult> OnPostReviseAsync()
    {
        if (Input.Id == 0)
        {
            ModelState.AddModelError("", "Save this quotation before creating a sub-quotation.");
            await ReloadImagesAsync();
            return Page();
        }

        ReviseFromId = Input.Id;
        Input.JobNo = await NextRevisionJobNoAsync(Input.Id);
        Input.Id = 0;                                            // force an insert

        return await SaveAsync();
    }

    private async Task<IActionResult> SaveAsync()
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

        // Revisions stay a flat family under the original: revising 0005.3 yields 0005.4,
        // never 0005.3.2. So the link always points at the root, whichever one was opened.
        var newParentId = ReviseFromId is int copiedFrom ? await RootIdAsync(copiedFrom) : (int?)null;

        if (Input.Id == 0)
        {
            target = new Quotation { CreatedAt = DateTime.UtcNow, ParentId = newParentId };
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

        if (ReviseFromId is int sourceId)
        {
            // A revision starts with its own copies of the parent's images. Sharing the
            // paths would mean deleting one here also destroys the original's file.
            var sourceImages = await db.QuotationImages
                .Where(i => i.QuotationId == sourceId)
                .OrderBy(i => i.SortOrder)
                .AsNoTracking()
                .ToListAsync();

            foreach (var source in sourceImages)
            {
                var edit = ExistingImages.FirstOrDefault(e => e.Id == source.Id);
                if (edit is { Delete: true }) continue;

                var copied = CopyUploadedFile(source.Path);
                if (copied is null) continue;               // source vanished from disk

                target.Images.Add(new QuotationImage
                {
                    Path = copied,
                    Caption = edit is null ? source.Caption : Blank(edit.Caption),
                    SortOrder = source.SortOrder
                });
            }
        }
        else
        {
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

        Flash = ReviseFromId is null
            ? $"Quotation {target.JobNo} saved."
            : $"Sub-quotation {target.JobNo} created. The original is unchanged.";
        return RedirectToPage("/Quotations/Edit", new { id = target.Id });
    }

    private async Task ReloadImagesAsync()
    {
        // On a revision the images still belong to the parent until the save succeeds.
        var owner = ReviseFromId ?? Input.Id;

        Images = owner == 0
            ? []
            : await db.QuotationImages
                .Where(i => i.QuotationId == owner)
                .OrderBy(i => i.SortOrder)
                .AsNoTracking()
                .ToListAsync();

        if (ReviseFromId is int copiedFrom)
        {
            var rootId = await RootIdAsync(copiedFrom);
            ParentJobNo = await JobNoAsync(rootId);
            CopiedFromJobNo = rootId == copiedFrom ? null : await JobNoAsync(copiedFrom);
        }
    }

    /// <summary>Suggests the next sequential job number, e.g. INV-2026-0007.</summary>
    private async Task<string> NextJobNoAsync()
    {
        var year = DateTime.Today.Year;
        var prefix = $"INV-{year}-";

        var numbers = await db.Quotations
            .Where(q => q.JobNo.StartsWith(prefix))
            .Select(q => q.JobNo)
            .ToListAsync();

        // Revisions carry a dotted suffix ("0007.2") and must not feed the root counter,
        // otherwise int.TryParse fails and the sequence silently restarts at 0001.
        var next = numbers
            .Select(j => j[prefix.Length..])
            .Where(suffix => !suffix.Contains('.'))
            .Select(suffix => int.TryParse(suffix, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{next:D4}";
    }

    /// <summary>
    /// Next free sub-quotation number in the family, e.g. INV-2026-0004.2. Revising any
    /// member returns the next sibling — revising 0004.3 gives 0004.4, not 0004.3.2.
    /// </summary>
    private async Task<string> NextRevisionJobNoAsync(int fromId)
    {
        var rootId = await RootIdAsync(fromId);
        var rootJobNo = await JobNoAsync(rootId) ?? "";

        // Siblings are found by ParentId, so a hand-edited job number cannot skew the count.
        var siblings = await db.Quotations
            .Where(q => q.ParentId == rootId)
            .Select(q => q.JobNo)
            .ToListAsync();

        var marker = rootJobNo + ".";
        var next = siblings
            .Where(j => j.StartsWith(marker))
            .Select(j => j[marker.Length..])
            .Select(suffix => int.TryParse(suffix.Split('.')[0], out var n) ? n : 1)
            .DefaultIfEmpty(1)                            // the original counts as .1
            .Max() + 1;

        return $"{rootJobNo}.{next}";
    }

    /// <summary>Walks up to the original a quotation belongs to; returns the id itself if it is one.</summary>
    private async Task<int> RootIdAsync(int id)
    {
        // Guarded loop rather than a single hop, in case older nested data exists.
        for (var hop = 0; hop < 10; hop++)
        {
            var parentId = await db.Quotations
                .Where(q => q.Id == id)
                .Select(q => q.ParentId)
                .FirstOrDefaultAsync();

            if (parentId is null) return id;
            id = parentId.Value;
        }
        return id;
    }

    private Task<string?> JobNoAsync(int id) =>
        db.Quotations.Where(q => q.Id == id).Select(q => q.JobNo).FirstOrDefaultAsync();

    /// <summary>Duplicates an upload so a revision never shares a file with its parent.</summary>
    private string? CopyUploadedFile(string webPath)
    {
        try
        {
            var source = Path.Combine(env.WebRootPath, webPath.TrimStart('/'));
            if (!System.IO.File.Exists(source)) return null;

            var uploads = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            var name = $"{Guid.NewGuid():N}{Path.GetExtension(source)}";
            System.IO.File.Copy(source, Path.Combine(uploads, name));
            return $"/uploads/{name}";
        }
        catch (IOException)
        {
            return null;   // a missing image is not worth failing the whole save over
        }
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
