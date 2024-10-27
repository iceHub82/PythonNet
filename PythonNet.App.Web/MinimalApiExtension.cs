using CSnakes.Runtime;

namespace PythonNet.App.Web;

public static class MinimalApiExtension
{
    public static void MinimalApi(this WebApplication app)
    {
        app.MapGet("/api/dashboard/stock", (IPlot plot, int tickerDd, DateTime startDate, DateTime endDate) => 
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

            plot.ReadCsvPlotAndSave(stock.Ticker!, stock.Title!, stock.FileName!, start, end);

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