using System.ComponentModel.DataAnnotations;

namespace Inova.Quotations.Models;

public class QuotationImage
{
    public int Id { get; set; }

    public int QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Web path under wwwroot, e.g. "/uploads/3f9c....jpg".</summary>
    [Required]
    [StringLength(300)]
    public string Path { get; set; } = "";

    [StringLength(300)]
    public string? Caption { get; set; }
}
