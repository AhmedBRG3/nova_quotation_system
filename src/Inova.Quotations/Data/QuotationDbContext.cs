using Inova.Quotations.Models;
using Microsoft.EntityFrameworkCore;

namespace Inova.Quotations.Data;

public class QuotationDbContext(DbContextOptions<QuotationDbContext> options) : DbContext(options)
{
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<QuotationImage> QuotationImages => Set<QuotationImage>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Quotation>(e =>
        {
            e.ToTable("quotations");
            e.HasIndex(q => q.JobNo).IsUnique();
            e.Property(q => q.VatPercent).HasPrecision(5, 2);

            e.HasMany(q => q.Items)
             .WithOne(i => i.Quotation)
             .HasForeignKey(i => i.QuotationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(q => q.Images)
             .WithOne(i => i.Quotation)
             .HasForeignKey(i => i.QuotationId)
             .OnDelete(DeleteBehavior.Cascade);

            // Deleting an original must never take its revisions with it.
            e.HasOne(q => q.Parent)
             .WithMany(q => q.Revisions)
             .HasForeignKey(q => q.ParentId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<QuotationItem>(e =>
        {
            e.ToTable("quotation_items");
            e.Property(i => i.Quantity).HasPrecision(12, 2);
            e.Property(i => i.UnitPrice).HasPrecision(14, 2);
        });

        b.Entity<QuotationImage>(e => e.ToTable("quotation_images"));

        b.Entity<CompanyProfile>(e =>
        {
            e.ToTable("company_profile");
            e.Property(c => c.DefaultVatPercent).HasPrecision(5, 2);
        });
    }
}
