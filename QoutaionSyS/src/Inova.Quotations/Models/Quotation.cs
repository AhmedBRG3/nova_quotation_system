using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inova.Quotations.Models;

public class Quotation
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Job No. is required")]
    [StringLength(40)]
    [Display(Name = "Job No.")]
    public string JobNo { get; set; } = "";

    [Required(ErrorMessage = "Subject is required")]
    [StringLength(300)]
    [Display(Name = "Subject (English)")]
    public string SubjectEn { get; set; } = "";

    [StringLength(300)]
    [Display(Name = "Subject (Arabic)")]
    public string? SubjectAr { get; set; }

    [Required(ErrorMessage = "Date is required")]
    [Display(Name = "Date")]
    public DateOnly QuoteDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Company name is required")]
    [StringLength(200)]
    [Display(Name = "Company (English)")]
    public string CompanyEn { get; set; } = "";

    [StringLength(200)]
    [Display(Name = "Company (Arabic)")]
    public string? CompanyAr { get; set; }

    [Required(ErrorMessage = "Contact person is required")]
    [StringLength(200)]
    [Display(Name = "Contact person (English)")]
    public string ContactPersonEn { get; set; } = "";

    [StringLength(200)]
    [Display(Name = "Contact person (Arabic)")]
    public string? ContactPersonAr { get; set; }

    [StringLength(200)]
    [Display(Name = "Job title / phone")]
    public string? ContactDetails { get; set; }

    [Range(0, 100)]
    [Display(Name = "VAT %")]
    public decimal VatPercent { get; set; } = 14m;

    /// <summary>
    /// One clause per line. Split on '|' gives the English and Arabic halves:
    /// "This quotation is valid for 30 days. | هذا العرض صالح لمدة ٣٠ يوماً."
    /// </summary>
    [Required(ErrorMessage = "Terms &amp; conditions are required")]
    [Display(Name = "Terms & Conditions")]
    public string Terms { get; set; } = "";

    [Display(Name = "Note")]
    public string? NoteEn { get; set; }

    [Display(Name = "Note (Arabic)")]
    public string? NoteAr { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when this quotation is a sub-quotation (revision) of another, e.g. INV-2026-0004.2.</summary>
    public int? ParentId { get; set; }
    public Quotation? Parent { get; set; }

    /// <summary>Sub-quotations derived from this one.</summary>
    public List<Quotation> Revisions { get; set; } = [];

    public List<QuotationItem> Items { get; set; } = [];
    public List<QuotationImage> Images { get; set; } = [];

    // ---- Computed money. Never stored: always derived from the item rows. ----

    [NotMapped]
    public decimal TotalExclVat => Math.Round(Items.Sum(i => i.LineTotal), 2, MidpointRounding.AwayFromZero);

    [NotMapped]
    public decimal VatAmount => Math.Round(TotalExclVat * VatPercent / 100m, 2, MidpointRounding.AwayFromZero);

    [NotMapped]
    public decimal TotalInclVat => TotalExclVat + VatAmount;

    /// <summary>Terms text parsed into (English, Arabic) pairs for the two-column print grid.</summary>
    [NotMapped]
    public IEnumerable<(string En, string Ar)> TermLines =>
        (Terms ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split('|', 2);
                return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : "");
            });
}
