using System.ComponentModel.DataAnnotations;

namespace Inova.Quotations.Models;

/// <summary>Single-row table holding the agency's own details and the defaults for new quotations.</summary>
public class CompanyProfile
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "Company name")]
    public string Name { get; set; } = "NOVA Marketing & Advertising Agency";

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(60)]
    public string? Phone { get; set; }

    [StringLength(120)]
    public string? Email { get; set; }

    [StringLength(120)]
    public string? Website { get; set; }

    /// <summary>Agency's tax registration number. Printed in the quotation masthead when set.</summary>
    [StringLength(60)]
    [Display(Name = "Tax registration number")]
    public string? TaxNumber { get; set; }

    /// <summary>Web path to the logo under wwwroot. Falls back to the CSS wordmark when empty.</summary>
    [StringLength(300)]
    [Display(Name = "Logo path")]
    public string? LogoPath { get; set; }

    [Range(0, 100)]
    [Display(Name = "Default VAT %")]
    public decimal DefaultVatPercent { get; set; } = 14m;

    /// <summary>Loaded into every new quotation, then editable per job. One clause per line, "English | Arabic".</summary>
    [Display(Name = "Default terms & conditions")]
    public string DefaultTerms { get; set; } = "";
}
