using CSnakes.Runtime;

namespace PythonNet.App.Web;

public static class MinimalApiExtension
{
    public static void MinimalApi(this WebApplication app)
    {
        app.MapGet("/api/dashboard/stock", (IUtil util, IPlot plotter, int tickerDd, DateTime startDate, DateTime endDate, string? openCb, string? closeCb, string? theme) =>
            StockPlot(tickerDd, startDate, endDate, (stock, start, end, plotName) =>
            {
                var data = util.ReadCsv(stock.FileName);
                var filtered = util.DateFilter(data, start, end);
                var sorted = util.SortData(filtered, "Date", true);
                var plot = plotter.Plot(sorted, stock.Ticker, stock.Title, openCb!, closeCb!, start, end,
                    theme == "dark" ? "dark" : "light");

                util.SavePlot(plot, PlotDirectory + Path.DirectorySeparatorChar, plotName, "png");
            }));

        // TODO: work in progress - fetches live data but does not plot it yet.
        app.MapGet("/api/dashboard/backtest", (IUtil util) =>
        {
            var data = util.DownloadData("AAPL", "2024-10-20", "2024-10-30");

            return Content("<img class='img-fluid' src='/dynamic-plots/plot_.png' alt='PNG Image'>");
        });
    }

    internal static readonly IReadOnlyDictionary<int, Stock> Tickers = new Dictionary<int, Stock>
    {
        [1] = new("AAPL", "Apple", "aapl_27.10.24-24.10.24.csv"),
        [2] = new("MSFT", "Microsoft", "msft_27.10.24-24.10.24.csv"),
        [3] = new("IBM", "IBM", "ibm_27.10.24-24.10.24.csv"),
    };

    internal const string PlotDirectory = "wwwroot/plots";

    /// <summary>
    /// Validates the request, hands a unique output name to <paramref name="savePlot"/>, and returns the
    /// HTMX fragment pointing at it. <paramref name="savePlot"/> is the only seam - it lets the validation,
    /// naming and failure branches be tested without booting a Python runtime.
    /// </summary>
    internal static IResult StockPlot(int tickerDd, DateTime startDate, DateTime endDate,
        Action<Stock, string, string, string> savePlot)
    {
        if (!Tickers.TryGetValue(tickerDd, out var stock))
            return Results.BadRequest($"Unknown ticker '{tickerDd}'. Expected one of: {string.Join(", ", Tickers.Keys)}.");

        if (startDate > endDate)
            return Results.BadRequest("startDate must be on or before endDate.");

        // Unique per request: a fixed plot_{ticker}.png let concurrent callers overwrite each other's
        // chart between SavePlot and the browser's follow-up GET.
        var plotName = $"plot_{stock.Ticker}_{Guid.NewGuid():N}";

        try
        {
            savePlot(stock, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), plotName);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Could not plot {stock.Ticker}: {ex.Message}", statusCode: 500);
        }

        SweepStalePlots();

        return Content($"<img class='img-fluid' src='/dynamic-plots/{plotName}.png' alt='PNG Image'>");
    }

    /// <summary>
    /// Each PNG is fetched once, right after the response. Drop anything the browser has long since had.
    /// </summary>
    // ponytail: swept inline on the next request rather than by a background service - it is a handful
    // of File.Delete calls. Move it to a hosted service if the plots directory ever gets big enough to stat.
    internal static void SweepStalePlots(TimeSpan? maxAge = null)
    {
        if (!Directory.Exists(PlotDirectory)) return;

        var cutoff = DateTime.UtcNow - (maxAge ?? TimeSpan.FromMinutes(5));

        foreach (var file in Directory.EnumerateFiles(PlotDirectory, "plot_*.png"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff) File.Delete(file);
            }
            catch (IOException)
            {
                // Raced with another request still serving it; the next sweep gets it.
            }
        }
    }

    private static IResult Content(string content)
    {
        return Results.Content(content, "text/html");
    }
}

internal record Stock(string Ticker, string Title, string FileName);