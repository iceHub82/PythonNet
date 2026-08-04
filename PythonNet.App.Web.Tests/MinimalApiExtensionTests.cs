using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace PythonNet.App.Web.Tests;

public class MinimalApiExtensionTests
{
    private static readonly DateTime Start = new(2024, 10, 24);
    private static readonly DateTime End = new(2024, 10, 27);

    // Renders nothing but a marker so the assertions can tell "reached Python" from "rejected early".
    private static string Marker(Stock s, string start, string end) => $"{s.Ticker}|{s.FileName}|{start}|{end}";

    [Theory]
    [InlineData(1, "AAPL", "Apple", "aapl_27.10.24-24.10.24.csv")]
    [InlineData(2, "MSFT", "Microsoft", "msft_27.10.24-24.10.24.csv")]
    [InlineData(3, "IBM", "IBM", "ibm_27.10.24-24.10.24.csv")]
    public void KnownTicker_RendersWithResolvedStock(int id, string ticker, string title, string file)
    {
        var result = MinimalApiExtension.StockPlot(id, Start, End, Marker);

        var content = Assert.IsType<ContentHttpResult>(result);
        Assert.Equal($"{ticker}|{file}|2024-10-24|2024-10-27", content.ResponseContent);
        Assert.Equal("text/html", content.ContentType);
        Assert.Equal(title, MinimalApiExtension.Tickers[id].Title);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(99)]
    public void UnknownTicker_IsRejectedWithoutCallingPython(int id)
    {
        var called = false;

        var result = MinimalApiExtension.StockPlot(id, Start, End, (_, _, _) =>
        {
            called = true;
            return "should not happen";
        });

        var bad = Assert.IsType<BadRequest<string>>(result);
        Assert.Contains($"Unknown ticker '{id}'", bad.Value);
        Assert.False(called);
    }

    [Fact]
    public void StartAfterEnd_IsRejected()
    {
        var result = MinimalApiExtension.StockPlot(1, End, Start, Marker);

        var bad = Assert.IsType<BadRequest<string>>(result);
        Assert.Contains("startDate must be on or before endDate", bad.Value);
    }

    [Fact]
    public void SameStartAndEnd_IsAllowed()
    {
        var result = MinimalApiExtension.StockPlot(1, Start, Start, Marker);

        Assert.IsType<ContentHttpResult>(result);
    }

    [Fact]
    public void RenderFailure_BecomesProblemResponse()
    {
        var result = MinimalApiExtension.StockPlot(1, Start, End,
            (_, _, _) => throw new FileNotFoundException("Data/aapl_27.10.24-24.10.24.csv"));

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
        Assert.Contains("Could not plot AAPL", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void EveryTickerMapsToAShippedCsv()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");

        foreach (var stock in MinimalApiExtension.Tickers.Values)
            Assert.True(File.Exists(Path.Combine(dataDir, stock.FileName)), $"missing CSV for {stock.Ticker}");
    }
}
