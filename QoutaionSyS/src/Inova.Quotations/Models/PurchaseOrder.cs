using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inova.Quotations.Models;

/// <summary>
/// An order placed with a vendor to procure what a quotation promised the client.
/// One quotation can spawn several, e.g. one per vendor.
/// </summary>
public class PurchaseOrder
{
    public int Id { get; set; }

    [Required(ErrorMessage = "PO No. is required")]
    [StringLength(40)]
    [Display(Name = "PO No.")]
    public string PoNo { get; set; } = "";

    [Required(ErrorMessage = "Date is required")]
    [Display(Name = "Date")]
    public DateOnly PoDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// The quotation this order procures for. Nullable, and nulled rather than cascaded when
    /// that quotation is deleted — a placed order still happened and must not vanish with it.
    /// </summary>
    public int? QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    [Required(ErrorMessage = "Subject is required")]
    [StringLength(300)]
    [Display(Name = "Subject (English)")]
    public string SubjectEn { get; set; } = "";

    [StringLength(300)]
    [Display(Name = "Subject (Arabic)")]
    public string? SubjectAr { get; set; }

    // ---- Vendor. Free text, exactly as the client details are on a quotation. ----

    [Required(ErrorMessage = "Vendor name is required")]
    [StringLength(200)]
    [Display(Name = "Vendor (English)")]
    public string VendorNameEn { get; set; } = "";

    [StringLength(200)]
    [Display(Name = "Vendor (Arabic)")]
    public string? VendorNameAr { get; set; }

    [StringLength(200)]
    [Display(Name = "Contact person (English)")]
    public string? VendorContactPersonEn { get; set; }

    [StringLength(200)]
    [Display(Name = "Contact person (Arabic)")]
    public string? VendorContactPersonAr { get; set; }

    [StringLength(200)]
    [Display(Name = "Job title / phone")]
    public string? VendorContactDetails { get; set; }

    [StringLength(60)]
    [Display(Name = "Vendor tax number")]
    public string? VendorTaxNumber { get; set; }

    [Range(0, 100)]
    [Display(Name = "VAT %")]
    public decimal VatPercent { get; set; } = 14m;

    /// <summary>One clause per line, "English | Arabic" — see <see cref="TermsText"/>.</summary>
    [Required(ErrorMessage = "Terms &amp; conditions are required")]
    [Display(Name = "Terms & Conditions")]
    public string Terms { get; set; } = "";

    [Display(Name = "Note")]
    public string? NoteEn { get; set; }

    [Display(Name = "Note (Arabic)")]
    public string? NoteAr { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<PurchaseOrderItem> Items { get; set; } = [];

    // ---- Computed money. Never stored: always derived from the item rows. ----

    [NotMapped]
    public decimal TotalExclVat => Math.Round(Items.Sum(i => i.LineTotal), 2, MidpointRounding.AwayFromZero);

    [NotMapped]
    public decimal VatAmount => Math.Round(TotalExclVat * VatPercent / 100m, 2, MidpointRounding.AwayFromZero);

    [NotMapped]
    public decimal TotalInclVat => TotalExclVat + VatAmount;

    /// <summary>Terms text parsed into (English, Arabic) pairs for the two-column print grid.</summary>
    [NotMapped]
    public IEnumerable<(string En, string Ar)> TermLines => TermsText.Parse(Terms);
}
