using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Inova.Quotations.Services;

/// <summary>
/// Renders the print page to PDF with headless Chrome, so the download is pixel-identical
/// to the on-screen A4 preview. Chrome is downloaded once on first use.
/// </summary>
public class PdfService(ILogger<PdfService> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IBrowser? _browser;

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is { IsClosed: false }) return _browser;

        await _gate.WaitAsync();
        try
        {
            if (_browser is { IsClosed: false }) return _browser;

            logger.LogInformation(
                "Ensuring a headless Chrome build is available… the first run downloads ~150 MB and can take several minutes.");
            var fetcher = new BrowserFetcher();
            var installed = await fetcher.DownloadAsync();
            logger.LogInformation("Using Chrome build {Build} at {Path}.", installed.BuildId, installed.GetExecutablePath());

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                // The print page is served by this same app over the ASP.NET dev certificate,
                // which Chrome would otherwise refuse when the app is running on HTTPS.
                AcceptInsecureCerts = true,
                Args = ["--no-sandbox", "--font-render-hinting=none", "--ignore-certificate-errors"]
            });
            return _browser;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<byte[]> RenderAsync(string url, CancellationToken ct = default)
    {
        var browser = await GetBrowserAsync();
        await using var page = await browser.NewPageAsync();

        await page.GoToAsync(url, new NavigationOptions
        {
            WaitUntil = [WaitUntilNavigation.Networkidle0],
            Timeout = 60_000
        });

        return await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            // The stylesheet already reserves the page margins, so Chrome adds none.
            MarginOptions = new MarginOptions { Top = "0", Right = "0", Bottom = "0", Left = "0" }
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }
}
