using Inova.Quotations.Data;
using Inova.Quotations.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<QuotationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<PdfService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// Downloads the quotation as a PDF by rendering the very same print page in headless Chrome.
app.MapGet("/quotations/{id:int}/pdf", async (
    int id,
    HttpContext http,
    QuotationDbContext db,
    PdfService pdf,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var jobNo = await db.Quotations
        .Where(q => q.Id == id)
        .Select(q => q.JobNo)
        .FirstOrDefaultAsync(ct);

    if (jobNo is null) return Results.NotFound();

    // "bare=true" strips the on-screen toolbar so it never lands in the PDF.
    var printUrl = $"{http.Request.Scheme}://{http.Request.Host}/Quotations/Print/{id}?bare=true";

    var logger = loggerFactory.CreateLogger("PdfDownload");
    try
    {
        var bytes = await pdf.RenderAsync(printUrl, ct);
        var safeJobNo = string.Concat(jobNo.Split(Path.GetInvalidFileNameChars()));
        return Results.File(bytes, "application/pdf", $"Quotation-{safeJobNo}.pdf");
    }
    catch (Exception ex)
    {
        // Without this the browser just shows a blank 500 and the real cause never reaches the log.
        logger.LogError(ex, "PDF render failed for quotation {Id} at {Url}.", id, printUrl);
        return Results.Problem(
            title: "Could not generate the PDF",
            detail: $"{ex.GetType().Name}: {ex.Message}",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

// Uploaded images live under wwwroot/uploads.
Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "uploads"));

await DbInitializer.InitialiseAsync(app.Services);

app.Run();
