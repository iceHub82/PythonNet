[![My Skills](https://skillicons.dev/icons?i=dotnet,py,htmx,js)](https://skillicons.dev)

## Project Overview

This project demonstrates how to call Python scripts from a .NET 9 Razor Pages web application, using the recently published [CSnakes](https://github.com/tonybaloney/CSnakes) library to enable Python execution within .NET.

### Solution Details

- The application is built on .NET 9 Razor Pages and integrates with **CSnakes**, allowing seamless Python script execution within .NET.
- A minimal API endpoint is used, together with **HTMX**, to call Python scripts and return dynamically generated stock graph images (PNG format) using the **matplotlib** and **pandas** libraries in Python.

## Prerequisites

- **.NET 9 SDK** (the solution targets `net9.0`)
- **Python 3.12** — optional. CSnakes is configured to fall back to a NuGet-supplied
  CPython 3.12.4, so a local install is only needed if you want to run the Python
  tests directly, or if you set `Python3_ROOT_DIR`.

## Running the app

```bash
cd PythonNet.App.Web
dotnet run
```

Then open the URL printed in the console (see `Properties/launchSettings.json`).

On first start CSnakes creates `PythonNet.Python/.venv` and pip-installs
`PythonNet.Python/requirements.txt` into it. This takes a minute or two; progress
is visible in the console output. Subsequent starts reuse the venv.

> Run from `PythonNet.App.Web`, not the solution root — `Program.cs` defaults the
> Python home to `<cwd>/../PythonNet.Python`, and `util.read_csv` resolves data
> files as `Data/<file>` relative to the working directory.

To point at a Python home elsewhere (a published layout, a shared venv), override
either setting via `appsettings.json` or the environment:

```bash
Python__Home=/srv/pythonnet Python__VirtualEnvironment=/srv/venv dotnet run
```

## Running the tests

```bash
dotnet test PythonNetSolution.sln            # xUnit: endpoint validation + routing
cd PythonNet.Python && pip install -r requirements.txt pytest && python -m pytest
```

Both are run by `.github/workflows/build.yml` on every push and pull request.

## How the .NET ↔ Python wiring works

1. **`PythonNet.Python`** holds the Python sources. Each one is listed as an
   `<AdditionalFiles>` entry in `PythonNet.Python.csproj`, which is what makes the
   CSnakes source generator pick it up.
2. At build time the generator reads the type-annotated function signatures in
   `util.py` and `plot.py` and emits matching C# interfaces — `IUtil` and `IPlot` —
   plus `IPythonEnvironment` extension methods (`.Util()`, `.Plot()`) that return them.
3. **`Program.cs`** configures the runtime (`WithHome`, `WithVirtualEnvironment`,
   `FromNuGet("3.12.4")`, `WithPipInstaller`) and registers `IUtil`/`IPlot` as
   singletons, so they can be injected anywhere.
4. **`MinimalApiExtension.cs`** injects them into `GET /api/dashboard/stock`, which
   validates its query parameters, then chains
   `read_csv → date_filter → sort_data → plot → save_plot` and returns an `<img>`
   fragment for HTMX to swap into the page. Each request renders to its own
   `wwwroot/plots/plot_{ticker}_{guid}.png` — served from `/dynamic-plots` with
   caching disabled — so concurrent callers cannot overwrite each other's chart.
   Files older than five minutes are swept on the next request.

Adding a Python function is therefore: annotate it, rebuild, call the generated
C# method. Adding a *new* `.py` file also needs an `<AdditionalFiles>` entry.

## Sample data

`PythonNet.App.Web/Data/` ships three static CSVs (`Date,Close,Open,High,Low`)
covering 24–27 Oct 2024 for AAPL, MSFT and IBM, so the dashboard works offline with
no market-data API key. The dropdown values map to them in
`MinimalApiExtension.Tickers`; add a ticker by dropping a CSV in `Data/`, adding a
`<None Update>` copy entry in `PythonNet.App.Web.csproj`, and adding one line to
that dictionary and to `IndexModel.Tickers`.
