# Transit

A self-contained Blazor web application that automates the monthly commute-kilometre
(woon-werkverkeer) declaration. It runs locally on your work laptop at `http://localhost:83`
and handles the entire flow — from marking attendance day by day, through reviewing a travel
route for each travel day, to filling the employer-provided Excel template and converting it
to a signed-ready PDF.

No external services, no secrets, no network dependency. Everything is stored in a local
SQLite database and a folder on disk.

---

![Attendance calendar — Transit's most distinctive screen](assets/images/screenshot-attendance.png)

*Attendance calendar — brush-paint your work location per day, then generate the declaration
from the review grid.*

---

## Features

- **Brush-model calendar** — select an active location (Home, Office, or any custom location)
  and click days to paint them. Clicking a painted day clears it.
- **Review grid** — pull a full month, see every weekday defaulted and editable, assign a
  travel route per travel day, then generate.
- **Excel + PDF generation** — fills the employer's own `.xlsx` template in place (preserving
  formatting and formulas) and converts it to PDF via LibreOffice headless.
- **Configurable template mapping** — map data fields to columns and static header cells
  through the UI; survives the employer changing the sheet layout.
- **Append-only history** — every generation creates an immutable run with its `.xlsx` and
  `.pdf`; re-running a month creates a new run and never overwrites the old one.
- **History preview** — open any past run in a read-only modal that renders the stored sheet,
  formula totals included.
- **User-defined work locations** — arbitrary named locations with a colour, a travel flag,
  an optional postcode (used for the template's `van`/`naar` columns), and exactly one
  designated as the default for unmarked days.
- **Directed routes** — routes connect two locations with separate outbound and return km,
  handling real-world asymmetry. A per-destination default is pre-selected in the review grid.

## Requirements

- Windows (IIS).
- .NET 10 SDK (installed by the script if missing).
- ASP.NET Core Hosting Bundle 10 (installed by the script if missing).
- LibreOffice (installed via Chocolatey if missing; required for PDF conversion).
- Chocolatey — only needed if LibreOffice is not already installed.

## Installation

The repository ships with `scripts\Install-Transit.ps1`, an idempotent PowerShell script that
provisions everything on a bare Windows machine and deploys the app. It is also the
recommended way to update an existing installation to the latest commit.

**What the script does:**

1. Self-elevates to Administrator.
2. Enables the required IIS Windows features.
3. Installs the .NET 10 SDK if absent.
4. Installs the ASP.NET Core Hosting Bundle if absent and bounces IIS to load the module.
5. Installs LibreOffice via Chocolatey if absent.
6. Creates (or reconciles) the `Transit` IIS app pool (No Managed Code) and the `transit`
   website bound to `http://localhost:83`.
7. Creates the data-root folder at `C:\ProgramData\Transit\Data` and grants the app-pool
   identity Modify access.
8. Runs the test suite — the deploy is aborted if any test fails.
9. Publishes the app to `C:\inetpub\wwwroot\Transit`.
10. Performs a smoke check against `http://localhost:83`.

**To install or update:**

```powershell
# From inside the cloned repository:
.\scripts\Install-Transit.ps1
```

The script is safe to re-run at any time. Steps that are already done are skipped. If the
.NET SDK or Hosting Bundle installer flags that a reboot is required (exit code 3010), the
script will tell you and you should reboot and re-run it before the app will work.

**Customising the data root:**

The default data location is `C:\ProgramData\Transit\Data`. To use a different path, edit
`src\Transit.Web\appsettings.json` before publishing:

```json
{
  "Transit": {
    "DataRoot": "D:\\YourPreferredPath\\Transit\\Data"
  }
}
```

Then also update the `$DataRoot` constant at the top of the install script so it creates and
ACLs the right folder.

## Durability and backup

`C:\ProgramData\Transit\Data` is the only copy of your attendance history and your signed-
declaration archive. It contains the SQLite database, uploaded templates, and every generated
`.xlsx` and `.pdf`. There is no in-app backup. Back up this folder to a second location
(a scheduled copy or an off-machine sync) so a laptop failure does not lose months of records.

## Build and run locally (development)

```powershell
dotnet restore
dotnet build
dotnet run --project src/Transit.Web
```

The app starts on the Kestrel default port. For database schema changes:

```powershell
dotnet ef migrations add <Name> `
    --project src/Transit.Infrastructure `
    --startup-project src/Transit.Web

dotnet ef database update `
    --project src/Transit.Infrastructure `
    --startup-project src/Transit.Web
```

## Tests

```powershell
dotnet test
```

Core domain logic (km computation, route and location default resolution, unmarked-day
defaulting, mapping resolution, travel-only export filtering) is unit-tested with no I/O.
Integration tests for the Excel writer and PDF converter require a real template fixture and
LibreOffice installed.

## Architecture overview

```
Transit.slnx
  src/
    Transit.Core            # domain entities, interfaces, pure services, km logic
    Transit.Infrastructure  # EF Core + SQLite, ClosedXML, LibreOffice, filesystem
    Transit.Web             # Blazor Server UI + composition root
  tests/
    Transit.Tests           # xUnit
```

Dependency direction: `Web -> Infrastructure -> Core`. `Core` has no I/O dependencies.
All filesystem, database, and subprocess work lives behind interfaces in `Infrastructure`.

For the full design rationale, open [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## License

Private — all rights reserved.
