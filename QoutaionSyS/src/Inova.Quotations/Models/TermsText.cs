namespace Inova.Quotations.Models;

/// <summary>
/// Shared parsing for the bilingual terms field used by quotations and purchase orders.
/// Both documents print the same two-column grid, so they must split identically.
/// </summary>
public static class TermsText
{
    /// <summary>
    /// One clause per line. Split on '|' gives the English and Arabic halves:
    /// "This quotation is valid for 30 days. | هذا العرض صالح لمدة ٣٠ يوماً."
    /// </summary>
    public static IEnumerable<(string En, string Ar)> Parse(string? terms) =>
        (terms ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split('|', 2);
                return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : "");
            });
}
