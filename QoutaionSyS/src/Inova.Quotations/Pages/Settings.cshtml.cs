using Inova.Quotations.Data;
using Inova.Quotations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Pages;

public class SettingsModel(QuotationDbContext db, IWebHostEnvironment env) : PageModel
{
    private static readonly string[] AllowedLogoExtensions = [".png", ".jpg", ".jpeg", ".webp", ".svg"];

    [BindProperty] public CompanyProfile Input { get; set; } = new();
    [BindProperty] public IFormFile? LogoFile { get; set; }

    [TempData] public string? Flash { get; set; }

    public async Task OnGetAsync() =>
        Input = await db.CompanyProfiles.AsNoTracking().FirstAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (LogoFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
            if (!AllowedLogoExtensions.Contains(ext))
                ModelState.AddModelError("LogoFile", "Logo must be a PNG, JPG, WEBP or SVG file.");
        }

        if (!ModelState.IsValid) return Page();

        var profile = await db.CompanyProfiles.FirstAsync();

        profile.Name = Input.Name.Trim();
        profile.Address = Input.Address?.Trim();
        profile.Phone = Input.Phone?.Trim();
        profile.Email = Input.Email?.Trim();
        profile.Website = Input.Website?.Trim();
        profile.TaxNumber = string.IsNullOrWhiteSpace(Input.TaxNumber) ? null : Input.TaxNumber.Trim();
        profile.DefaultVatPercent = Input.DefaultVatPercent;
        profile.DefaultTerms = Input.DefaultTerms.Trim();

        if (LogoFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
            var name = $"logo-{Guid.NewGuid():N}{ext}";
            var uploads = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            await using (var stream = System.IO.File.Create(Path.Combine(uploads, name)))
                await LogoFile.CopyToAsync(stream);

            profile.LogoPath = $"/uploads/{name}";
        }

        await db.SaveChangesAsync();
        Flash = "Settings saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearLogoAsync()
    {
        var profile = await db.CompanyProfiles.FirstAsync();
        profile.LogoPath = null;
        await db.SaveChangesAsync();
        Flash = "Logo removed — the printed sheet falls back to the text wordmark.";
        return RedirectToPage();
    }
}
