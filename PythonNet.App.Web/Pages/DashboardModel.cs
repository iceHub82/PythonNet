using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PythonNet.App.Web.Pages;

/// <summary>
/// Shared by Index and Backtest - both are the same controls over the same tickers. The ticker list is
/// projected from <see cref="MinimalApiExtension.Tickers"/> so the dropdown cannot drift from the ids
/// the endpoint actually accepts.
/// </summary>
public class DashboardModel : PageModel
{
    public IEnumerable<SelectListItem> Tickers { get; } = MinimalApiExtension.Tickers
        .Select(t => new SelectListItem(t.Value.Ticker, t.Key.ToString()))
        .ToList();

    public string StartDate { get; set; } = "2021-01-01";
    public string EndDate { get; set; } = "2023-01-01";
}
