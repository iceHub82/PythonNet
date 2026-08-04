using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace PythonNet.App.Web.Tests;

public class MinimalApiExtensionTests
{
    private static readonly DateTime Start = new(2024, 10, 24);
    private static readonly DateTime End = new(2024, 10, 27);

    /// <summary>Stands in for the Python call chain, recording what the endpoint asked it to render.</summary>
    private sealed class Recorder
    {
        public List<(Stock Stock, string Start, string End, string PlotName)> Calls { get; } = [];

        public void Save(Stock stock, string start, string end, string plotName) =>
            Calls.Add((stock, start, end, plotName));
    }

    [Theory]
    [InlineData(1, "AAPL", "Apple", "aapl_27.10.24-24.10.24.csv")]
    [InlineData(2, "MSFT", "Microsoft", "msft_27.10.24-24.10.24.csv")]
    [InlineData(3, "IBM", "IBM", "ibm_27.10.24-24.10.24.csv")]
    public void KnownTicker_PassesResolvedStockAndIsoDates(int id, string ticker, string title, string file)
    {
        var recorder = new Recorder();

        var result = MinimalApiExtension.StockPlot(id, Start, End, recorder.Save);

        var call = Assert.Single(recorder.Calls);
        Assert.Equal(ticker, call.Stock.Ticker);
        Assert.Equal(title, call.Stock.Title);
        Assert.Equal(file, call.Stock.FileName);
        Assert.Equal("2024-10-24", call.Start);
        Assert.Equal("2024-10-27", call.End);

        var content = Assert.IsType<ContentHttpResult>(result);
        Assert.Equal("text/html", content.ContentType);
        Assert.Contains($"/dynamic-plots/{call.PlotName}.png", content.ResponseContent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(99)]
    public void UnknownTicker_IsRejectedWithoutCallingPython(int id)
    {
        var recorder = new Recorder();

        var result = MinimalApiExtension.StockPlot(id, Start, End, recorder.Save);

        var bad = Assert.IsType<BadRequest<string>>(result);
        Assert.Contains($"Unknown ticker '{id}'", bad.Value);
        Assert.Empty(recorder.Calls);
    }

    [Fact]
    public void StartAfterEnd_IsRejectedWithoutCallingPython()
    {
        var recorder = new Recorder();

        var result = MinimalApiExtension.StockPlot(1, End, Start, recorder.Save);

        var bad = Assert.IsType<BadRequest<string>>(result);
        Assert.Contains("startDate must be on or before endDate", bad.Value);
        Assert.Empty(recorder.Calls);
    }

    [Fact]
    public void SameStartAndEnd_IsAllowed()
    {
        var result = MinimalApiExtension.StockPlot(1, Start, Start, new Recorder().Save);

        Assert.IsType<ContentHttpResult>(result);
    }

    [Fact]
    public void SaveFailure_BecomesProblemResponse()
    {
        var result = MinimalApiExtension.StockPlot(1, Start, End,
            (_, _, _, _) => throw new FileNotFoundException("Data/aapl_27.10.24-24.10.24.csv"));

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
        Assert.Contains("Could not plot AAPL", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void ConcurrentRequestsForOneTicker_GetDistinctPlotNames()
    {
        var recorder = new Recorder();

        var htmlA = (ContentHttpResult)MinimalApiExtension.StockPlot(1, Start, End, recorder.Save);
        var htmlB = (ContentHttpResult)MinimalApiExtension.StockPlot(1, Start, End, recorder.Save);

        var (first, second) = (recorder.Calls[0].PlotName, recorder.Calls[1].PlotName);
        Assert.StartsWith("plot_AAPL_", first);
        Assert.StartsWith("plot_AAPL_", second);
        Assert.NotEqual(first, second);
        Assert.NotEqual(htmlA.ResponseContent, htmlB.ResponseContent);
    }

    [Fact]
    public void SweepStalePlots_DeletesOnlyPlotsPastTheirAge()
    {
        var cwd = Directory.GetCurrentDirectory();
        var scratch = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.SetCurrentDirectory(scratch);
            var plots = Directory.CreateDirectory(MinimalApiExtension.PlotDirectory).FullName;

            var stale = Path.Combine(plots, "plot_AAPL_old.png");
            var fresh = Path.Combine(plots, "plot_AAPL_new.png");
            var other = Path.Combine(plots, "keep-me.txt");
            foreach (var f in new[] { stale, fresh, other }) File.WriteAllText(f, "x");
            File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddMinutes(-30));

            MinimalApiExtension.SweepStalePlots(TimeSpan.FromMinutes(5));

            Assert.False(File.Exists(stale));
            Assert.True(File.Exists(fresh));
            Assert.True(File.Exists(other));
        }
        finally
        {
            Directory.SetCurrentDirectory(cwd);
            Directory.Delete(scratch, recursive: true);
        }
    }

    [Fact]
    public void SweepStalePlots_IsANoOpWhenThereIsNoPlotDirectory()
    {
        var cwd = Directory.GetCurrentDirectory();
        var scratch = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.SetCurrentDirectory(scratch);
            MinimalApiExtension.SweepStalePlots();
            Assert.False(Directory.Exists(MinimalApiExtension.PlotDirectory));
        }
        finally
        {
            Directory.SetCurrentDirectory(cwd);
            Directory.Delete(scratch, recursive: true);
        }
    }

    [Fact]
    public void EveryTickerMapsToAShippedCsv()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");

        foreach (var stock in MinimalApiExtension.Tickers.Values)
            Assert.True(File.Exists(Path.Combine(dataDir, stock.FileName)), $"missing CSV for {stock.Ticker}");
    }
}
