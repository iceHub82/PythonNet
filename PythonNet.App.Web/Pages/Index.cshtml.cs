using CSnakes.Runtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PythonNet.App.Web.Pages;

public class IndexModel(IPythonEnvironment pythonEnv, ILogger<IndexModel> logger) : PageModel
{
    private readonly IPythonEnvironment _pythonEnv = pythonEnv;
    private readonly ILogger<IndexModel> _logger = logger;

    public string PythonOutput { get; set; }

    public void OnGet()
    {
        PythonOutput = _pythonEnv.Demo().HelloWorld("ZXWERTY");
    }
}