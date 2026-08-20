using Inova.Quotations.Models;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Data;

public static class DbInitializer
{
    public const string DefaultTerms = """
        This quotation is valid for 30 days from the date above. | هذا العرض صالح لمدة ٣٠ يوماً من التاريخ المذكور أعلاه.
        Payment: 50% advance on approval, 50% on final delivery. | الدفع: ٥٠٪ مقدماً عند الموافقة، و٥٠٪ عند التسليم النهائي.
        Work starts after signed approval and receipt of the advance payment. | يبدأ العمل بعد التوقيع على العرض وسداد الدفعة المقدمة.
        Two rounds of revisions are included per deliverable; extra rounds are billable. | يشمل العرض جولتي تعديلات لكل مُخرج، وأي تعديلات إضافية تُحتسب منفصلة.
        Any change of scope requires a written addendum to this quotation. | أي تغيير في نطاق العمل يتطلب ملحقاً كتابياً لهذا العرض.
        """;

    /// <summary>Applies migrations and makes sure the single company-profile row exists.</summary>
    public static async Task InitialiseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuotationDbContext>();

        await db.Database.MigrateAsync();

        if (!await db.CompanyProfiles.AnyAsync())
        {
            db.CompanyProfiles.Add(new CompanyProfile
            {
                Name = "NOVA Marketing & Advertising Agency",
                Address = "Address line, City, Egypt",
                Phone = "+20 000 000 0000",
                Email = "info@nova.agency",
                Website = "www.nova.agency",
                DefaultVatPercent = 14m,
                DefaultTerms = DefaultTerms
            });
            await db.SaveChangesAsync();
        }
    }
}
