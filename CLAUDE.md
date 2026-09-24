# CLAUDE.md

Guidance for Claude Code when implementing this repository. Read `ARCHITECTURE.md`
alongside this file — this is the operational how-to-work document; that is the design.

## Project

Working name: Transit. A single Blazor web app that automates the
monthly commute-kilometre (woon-werkverkeer) declaration. It captures per-day work location
on an in-app calendar, lets the user assign a travel route per travel day, fills an employer
Excel template, converts it to PDF, keeps a history, and offers downloads.

**Everything lives in this one app** — capture, storage, and declaration. There is no
external capture app, no network dependency, and no secrets. The app is always-local on the
work laptop at `localhost:83`.

## Working agreement — read this first

This project follows a constrained, inspect-before-build workflow:

- **Analyse and propose in chat before editing.** For any non-trivial change, explain what
  you'll do and why, and wait for approval. Do not make direct file edits without it.
- **Small, reviewable diffs.** One concern per change. No sweeping refactors bundled with
  features.
- **Ask before adding a dependency**, changing a public seam, or altering the data model.
- **State tradeoffs, not just conclusions.** Give the alternative you rejected and why.
- **Direct communication. No flattery, no filler.** If something is a bad idea, say so.
- Reach for the relevant skills where they fit: `/systematic-debugging` for diagnosis,
  `/never-nesting` before submitting nested control flow, `/frontend-design` for UI,
  `/brainstorming` for open design questions, `/git-writeup` for PRs and tags.

## Tech stack (pinned)

- **.NET 10 (LTS)**, C# latest.
- **Blazor Web App**, `InteractiveServer` render mode.
- **MudBlazor** for UI components; theme tokens defined via /frontend-design.
- **EF Core + SQLite** for attendance and all metadata.
- **ClosedXML** for reading/writing the Excel template.
- **LibreOffice headless** (subprocess) behind `IPdfConverter` for xlsx → pdf.
- **xUnit** for tests.
- Hosted on **IIS**, bound to `http://localhost:83` (loopback only).

Do not swap any of these without asking.

## Solution structure

```
Transit.slnx
  src/
    Transit.Core            # domain: entities, enums, interfaces, services, km logic
    Transit.Infrastructure  # EF Core, SQLite store, ClosedXML writer, LibreOffice, file store
    Transit.Web             # Blazor UI + composition root (DI, config)
  tests/
    Transit.Tests           # xUnit
```

**Dependency direction:** `Web → Infrastructure → Core`. `Core` depends on nothing but the
BCL — no EF, no ClosedXML. All I/O implementations live in `Infrastructure`.

## Build & run

```bash
dotnet restore
dotnet build
dotnet run --project src/Transit.Web

# EF Core migrations
dotnet ef migrations add <Name> --project src/Transit.Infrastructure --startup-project src/Transit.Web
dotnet ef database update            --project src/Transit.Infrastructure --startup-project src/Transit.Web

# tests
dotnet test

# publish for IIS
dotnet publish src/Transit.Web -c Release -o ./publish
```

## Conventions

- `Nullable` and `ImplicitUsings` enabled; treat warnings as errors is preferred.
- File-scoped namespaces; one top-level type per file.
- **Async end to end.** No `.Result`, `.Wait()`, or `async void` (except event handlers).
- Constructor injection; register services in `Web`'s composition root only.
- **Interface-first for the swappable seams** (see below). UI and services depend on the
  interface, never the concrete implementation.
- **`DateOnly` for calendar dates** — never `DateTime` for a declaration day.
- `record` types for DTOs and value-like domain objects.
- Guard clauses over nesting — keep control flow flat (`/never-nesting`).
- No business logic in Razor beyond binding; push it into `Core` services so it's testable.
- Logging via `ILogger<T>`; configuration via `IOptions<T>`.

## Key seams — do not bypass

```
IAttendanceStore       // read a date range; set/clear a day's location    (SqliteAttendanceStore)
IRouteStore            // route CRUD + per-destination default swap        (SqliteRouteStore)
IExcelWriter           // fill the template from a TemplateProfile         (ClosedXmlTemplateWriter)
IPdfConverter          // xlsx -> pdf                                      (LibreOfficePdfConverter)
ITemplateProfileStore  // persist template file + mappings, select active
IArtifactStore         // read/write/delete generated files on disk         (FileSystemArtifactStore)
ISheetPreviewReader    // read a sheet's used range for preview (evaluates formulas) (ClosedXmlSheetPreviewReader)
IDeclarationRunStore   // run metadata + frozen rows                        (SqliteDeclarationRunStore)
```

Interfaces live in `Core`; implementations in `Infrastructure`. Anything that touches the
filesystem, the database, or shells out goes behind one of these.

## Domain rules

- **Locations are user-defined** (`WorkLocation`), not a fixed enum. Each carries
  `CountsAsTravel`, an optional `Postcode` (feeds the template's `van`/`naar` postcode columns),
  and there is exactly one `IsDefaultForUnmarked` (Home). An optional `NetworkHint` (Wi-Fi name) lets the reminder toast suggest the location.
- **Km:** travel day → route `TotalKm` = `OutboundKm + ReturnKm` (outbound and return differ
  and are stored separately); non-travel day → `0`.
- **Routes are directed between-location pairs** (`FromLocationId` → `ToLocationId`) with separate
  `OutboundKm` / `ReturnKm` legs and a **per-destination** default. `RouteService` owns the rules
  over pure `RouteRules`; the pure `DeclarationBuilder` materialises the review grid (pull → default
  unmarked → resolve route by destination → km). The `/declaration` and `/routes` pages surface them.
- **Unmarked weekdays default to the `IsDefaultForUnmarked` location**, materialised as
  editable rows in the review grid. Nothing is silently zeroed.
- **Only `CountsAsTravel` rows are written to the sheet.** WFH/absent days never appear.
- **Do not compute monthly totals.** The template's own `SUM` formula owns them.
- **Template + export (`TemplateProfile`).** `ITemplateProfileStore` persists the uploaded
  `.xlsx` + its `ColumnMapping`/`CellMapping`s (one active profile). `ClosedXmlTemplateWriter`
  fills the template in place (travel-only rows, auto-inserting rows past `DataRowCount` so the
  `SUM` expands); `LibreOfficePdfConverter` makes the PDF. `GenerationService` orchestrates
  active-profile → draft → xlsx → pdf. The `/template` page configures it; `/declaration`
  generates. **Slice 4 generation is ephemeral** — bytes stream to the browser, nothing is
  persisted yet.
- **Generate & sign.** A mapped `SignatureName` cell makes Generate the signing act: the template is
  filled twice — unsigned for the stored xlsx, signed (name in a script font + today's date) for the
  PDF only. Never sign unattended; the signed workbook is never persisted.
- **Generation is append-only (slice 5, live).** Every Generate creates a new `DeclarationRun`
  (all reviewed rows frozen; `TemplateName` frozen; `PdfFilePath` null on an xlsx-only run) plus new
  artifacts under `generated\{yyyy-MM}\{runId}\`; re-running a month makes another run and never
  overwrites history. `GenerationService` threads the review grid's route overrides into generation
  and hands off to `DeclarationHistoryService` (over `IDeclarationRunStore` + `IArtifactStore`).
  Runs are immutable; a run can be hard-deleted (with confirm) from `/history`, which removes both
  the DB rows and the folder. No auto-retention. Backup is a folder-level sync of `DataRoot`.
- **Overview + history preview (post-slice-5).** The home page (`/`) is a status dashboard composed
  from existing services (no new backend): current-month counts, a previous-month declare nudge, a
  location breakdown, recent runs, and setup health. A history row opens a **read-only** modal that
  renders the run's stored `.xlsx` via `ISheetPreviewReader` — formula cells (the `SUM` total) are
  evaluated at render time; the stored files are never modified.

## Careful zones

- **Template fidelity:** load the uploaded template and write into it. Never rebuild the
  workbook from scratch — that destroys formatting, merged cells, and formulas.
- **No Office/COM automation.** `Microsoft.Office.Interop.Excel` is unsupported under IIS.
  PDF conversion only through `IPdfConverter`.
- **Loopback only.** The site binds to `localhost:83`; do not add external bindings or a
  network API. The reminder helper has two loopback endpoints: `today-status` (read) and
  `log-today` — the **only write endpoint**: today only, active locations only, never overwrites,
  loopback remote address enforced, JSON-only. Do not widen it; anything more goes through the UI.
- **Dates:** watch for any implicit timezone conversion that could shift a day.
- **Durability:** `C:\ProgramData\Transit\` is the only copy of attendance history — never write code that
  destructively rewrites it in place.
- **Theming is dual-source.** `wwwroot/tokens.css` is authoritative; `Theme/TransitTheme.cs`
  mirrors it by hand. Change one → change the other in the same diff, or MudBlazor components
  and custom `tr-` markup drift apart.

## Testing

- **Unit-test `Core` with no I/O:** km computation (`OutboundKm + ReturnKm`), route- and
  location-default resolution, unmarked-day defaulting, mapping resolution (field → cell),
  travel-only export filtering. These are pure and must stay pure.
- **Integration-test the writer/converter behind a flag** (they need a real template fixture
  and LibreOffice installed). Keep a small sample `.xlsx` fixture in the test project.
- A change that touches km logic, defaulting, or mapping resolution needs a test.

## Definition of done

Builds clean (no new warnings), tests green, diff reviewed and approved, and — if a seam, the
data model, or a decision changed — `ARCHITECTURE.md` updated in the same change.
