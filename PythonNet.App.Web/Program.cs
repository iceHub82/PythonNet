using Microsoft.Extensions.FileProviders;
using CSnakes.Runtime;
using PythonNet.App.Web;

var builder = WebApplication.CreateBuilder(args);

var pythonBuilder = builder.Services.WithPython();

// Overridable via appsettings or Python__Home / Python__VirtualEnvironment env vars, so the app can run
// from a published layout where the sources are not a sibling directory of the working directory.
var home = builder.Configuration["Python:Home"]
           ?? Path.Join(Environment.CurrentDirectory, "..", "PythonNet.Python");
var venv = builder.Configuration["Python:VirtualEnvironment"] ?? Path.Join(home, ".venv");
pythonBuilder
    .WithHome(home)
    .WithVirtualEnvironment(venv)
    .FromNuGet("3.12.4")
    .FromMacOSInstallerLocator("3.12")
    .FromEnvironmentVariable("Python3_ROOT_DIR", "3.12")
    .WithPipInstaller();

builder.Services.AddSingleton(sp => sp.GetRequiredService<IPythonEnvironment>().Util());
builder.Services.AddSingleton(sp => sp.GetRequiredService<IPythonEnvironment>().Plot());

builder.Services.AddRazorPages();

var app = builder.Build();

// Generated charts land here at runtime, so the directory may not exist on a fresh checkout.
var plots = Path.Combine(Directory.GetCurrentDirectory(), MinimalApiExtension.PlotDirectory);
Directory.CreateDirectory(plots);

app.UseStaticFiles(new StaticFileOptions{
    FileProvider = new PhysicalFileProvider(plots),
    RequestPath = "/dynamic-plots",
    OnPrepareResponse = r => {
        r.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        r.Context.Response.Headers["Pragma"] = "no-cache";
        r.Context.Response.Headers["Expires"] = "0";
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MinimalApi();

app.Run();