using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PythonNet.App.Web.Pages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;

    public List<object> Tickers { get; set; } = new();
    public string StartDate { get; set; }
    public string EndDate { get; set; }

    public void OnGet()
    {
        Tickers.Add(new { Id = 1, Name = "AAPL" });
        Tickers.Add(new { Id = 2, Name = "MSFT" });
        Tickers.Add(new { Id = 3, Name = "IBM" });

        StartDate = "01/01/2021";
        EndDate = "01/01/2023";
    }
}