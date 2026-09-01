using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inova.Quotations.Models;

public class QuotationItem
{
    public int Id { get; set; }

    public int QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public int SortOrder { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [StringLength(500)]
    [Display(Name = "Description (English)")]
    public string DescriptionEn { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Description (Arabic)")]
    public string? DescriptionAr { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Quantity must be 0 or more")]
    public decimal Quantity { get; set; } = 1m;

    [Range(0, 1_000_000_000, ErrorMessage = "Unit price must be 0 or more")]
    [Display(Name = "Unit price")]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
}
