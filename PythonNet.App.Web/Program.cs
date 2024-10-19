using CSnakes.Runtime;

var builder = WebApplication.CreateBuilder(args);

var pythonBuilder = builder.Services.WithPython();
var home = Path.Join(Environment.CurrentDirectory, "..", "PythonNet.Python");
var venv = Path.Join(home, ".venv");
pythonBuilder
    .WithHome(home)
    .WithVirtualEnvironment(venv)
    .FromNuGet("3.12.4")
    .FromMacOSInstallerLocator("3.12")
    .FromEnvironmentVariable("Python3_ROOT_DIR", "3.12")
    .WithPipInstaller();

builder.Services.AddSingleton(sp => sp.GetRequiredService<IPythonEnvironment>().Demo());
//builder.Services.AddSingleton(sp => sp.GetRequiredService<IPythonEnvironment>().TypeDemos());
//builder.Services.AddSingleton(sp => sp.GetRequiredService<IPythonEnvironment>().KmeansExample());

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

app.MapGet("/demo", (IDemo demo) => demo.HelloWorld("Karlsson"));

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

app.Run();
