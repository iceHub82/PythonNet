using CSnakes.Runtime;

namespace PythonNet.App.Web;

public static class MinimalApiExtension
{
    public static void MinimalApi(this WebApplication app)
    {
        app.MapGet("/api/dashboard/stock", (IUtil util, IPlot plot, int tickerDd, DateTime startDate, DateTime endDate, string? openCb, string? closeCb) => 
        {
            Stock stock = new();
            switch (tickerDd)
            {
                case 1: {
                        stock.Ticker = "AAPL";
                        stock.Title = "Apple";
                        stock.FileName = "aapl_27.10.24-24.10.24.csv";
                    }
                    break;
                case 2: {
                        stock.Ticker = "MSFT";
                        stock.Title = "Microsoft";
                        stock.FileName = "msft_27.10.24-24.10.24.csv";
                    }
                    break;
                case 3: {
                        stock.Ticker = "IBM";
                        stock.Title = "IBM";
                        stock.FileName = "ibm_27.10.24-24.10.24.csv";
                    }
                    break;
            }

            var start = startDate.ToString("yyyy-MM-dd");
            var end = endDate.ToString("yyyy-MM-dd");

            var dataFrame = util.ReadCsv(stock.FileName!);
            var filteredData = util.DateFilter(dataFrame, start, end);

            var plotResult = plot.Plot(filteredData, stock.Ticker!, stock.Title!, openCb!, closeCb!);

            util.SavePlot(plotResult, "wwwroot/plots/", $"plot_{stock.Ticker}", "png");

            var html = $"<img src='/dynamic-plots/plot_{stock.Ticker}.png' alt='PNG Image'>";

            return Content(html);
        });
    }

    private static IResult Content(string content)
    {
        return Results.Content(content, "text/html");
    }
}

class Stock
{
    public string? Ticker { get; set; }
    public string? Title { get; set; }
    public string? FileName { get; set; }
}