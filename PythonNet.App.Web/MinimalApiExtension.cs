using CSnakes.Runtime;

namespace PythonNet.App.Web;

public static class MinimalApiExtension
{
    public static void MinimalApi(this WebApplication app)
    {
        app.MapGet("/api/dashboard/stock", (IUtil util, IPlot plotter, int tickerDd, DateTime startDate, DateTime endDate, string? openCb, string? closeCb, string? theme) =>
            StockPlot(tickerDd, startDate, endDate, (stock, start, end) =>
            {
                var data = util.ReadCsv(stock.FileName);
                var filtered = util.DateFilter(data, start, end);
                var sorted = util.SortData(filtered, "Date", true);
                var plot = plotter.Plot(sorted, stock.Ticker, stock.Title, openCb!, closeCb!, start, end,
                    theme == "dark" ? "dark" : "light");

                // ponytail: one PNG per ticker, overwritten in place. Concurrent requests for the same
                // ticker race on this file; switch to a per-request name if this ever serves >1 user.
                util.SavePlot(plot, "wwwroot/plots/", $"plot_{stock.Ticker}", "png");

                return $"<img class='img-fluid' src='/dynamic-plots/plot_{stock.Ticker}.png' alt='PNG Image'>";
            }));

        app.MapGet("/api/dashboard/backtest", (IUtil util/*, IPlot plotter, int tickerDd, DateTime startDate, DateTime endDate, string? openCb, string? closeCb, string theme*/) =>
        {
            //Stock stock = new();
            //switch (tickerDd)
            //{
            //    case 1:
            //        {
            //            stock.Ticker = "AAPL";
            //            stock.Title = "Apple";
            //            stock.FileName = "aapl_27.10.24-24.10.24.csv";
            //        }
            //        break;
            //    case 2:
            //        {
            //            stock.Ticker = "MSFT";
            //            stock.Title = "Microsoft";
            //            stock.FileName = "msft_27.10.24-24.10.24.csv";
            //        }
            //        break;
            //    case 3:
            //        {
            //            stock.Ticker = "IBM";
            //            stock.Title = "IBM";
            //            stock.FileName = "ibm_27.10.24-24.10.24.csv";
            //        }
            //        break;
            //}

            //var start = startDate.ToString("yyyy-MM-dd");
            //var end = endDate.ToString("yyyy-MM-dd");

            //var data = util.ReadCsv(stock.FileName!);
            //var filtered = util.DateFilter(data, start, end);
            //var sorted = util.SortData(filtered, "Date", true);
            //var plot = plotter.Plot(sorted, stock.Ticker!, stock.Title!, openCb!, closeCb!, start, end, theme);

            //util.SavePlot(plot, "wwwroot/plots/", $"plot_{stock.Ticker}", "png");

            var data =  util.DownloadData("AAPL", "2024-10-20", "2024-10-30");

            //var html = $"<img class='img-fluid' src='/dynamic-plots/plot_{stock.Ticker}.png' alt='PNG Image'>";
            var html = $"<img class='img-fluid' src='/dynamic-plots/plot_.png' alt='PNG Image'>";


            return Content(html);
        });
    }

    internal static readonly IReadOnlyDictionary<int, Stock> Tickers = new Dictionary<int, Stock>
    {
        [1] = new("AAPL", "Apple", "aapl_27.10.24-24.10.24.csv"),
        [2] = new("MSFT", "Microsoft", "msft_27.10.24-24.10.24.csv"),
        [3] = new("IBM", "IBM", "ibm_27.10.24-24.10.24.csv"),
    };

    // ponytail: `render` is the only seam here - it lets the validation and failure branches be tested
    // without booting a Python runtime. Split into a service class if this grows past one call chain.
    internal static IResult StockPlot(int tickerDd, DateTime startDate, DateTime endDate,
        Func<Stock, string, string, string> render)
    {
        if (!Tickers.TryGetValue(tickerDd, out var stock))
            return Results.BadRequest($"Unknown ticker '{tickerDd}'. Expected one of: {string.Join(", ", Tickers.Keys)}.");

        if (startDate > endDate)
            return Results.BadRequest("startDate must be on or before endDate.");

        try
        {
            return Content(render(stock, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd")));
        }
        catch (Exception ex)
        {
            return Results.Problem($"Could not plot {stock.Ticker}: {ex.Message}", statusCode: 500);
        }
    }

    private static IResult Content(string content)
    {
        return Results.Content(content, "text/html");
    }
}

internal record Stock(string Ticker, string Title, string FileName);