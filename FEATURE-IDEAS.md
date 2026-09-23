# Feature ideas — Transit

_Generated 2026-09-23 from a read of the repo at `main` (f71ebc3). Proposals, not
commitments — pick what fits._

## Reading of the project

Transit turns "where was I each day" into a signed-ready commute declaration. The core loop is
**capture (brush calendar) → review (grid with route per travel day) → generate (template fill →
PDF → append-only run)**, for one user on one laptop. The architecture is clean and seam-heavy:
pure Core builders (`DeclarationBuilder`, `TravelKmCalculator`, `RouteRules`, `BrushResolver`),
I/O behind interfaces, and frozen `DeclarationRow` snapshots per run. The desktop reminder
(`Remind-Transit.ps1` + `/api/reminder/today-status`) is the only thing that reaches outside the
browser.

The gaps below cluster in two places: the **capture moment** (still a manual daily chore, with the
reminder only able to say "go open the app"), and the **space between a declaration and the
data it was made from** (nothing notices when they drift apart, and review choices are lost).

---

## Deep proposals

### 1. Stale-declaration detection ("this month changed since you declared it")

**The gap.** Every run freezes all reviewed rows (`DeclarationRow`, `SqliteDeclarationRunStore`),
and the current draft is always rebuildable (`DeclarationService.BuildDraftAsync`). Nothing ever
compares the two. You can paint a day in an already-declared month on `/attendance`, or change a
location in the review grid, and the History tile, the Home "to do" nudge
(`Home.razor` `_previousUndeclared`) and the Declaration page all stay silent. "Declared" is
currently binary: has a run or not. The maintainer likely doesn't see it because they know which
months they've touched.

**What it adds.** A month tile reads *Declared · 2 days changed since*. The Declaration page for a
declared month shows a diff strip ("12 Sep: Home → Office, +38.4 km"). Painting in a declared month
on `/attendance` shows a quiet banner. You find out before your employer does, and re-declaring is
a deliberate act with a clear reason.

**Fit & architecture**
- _Reuse:_ frozen rows via `DeclarationHistoryService`/`IDeclarationRunStore`; current draft via
  `DeclarationService`; `HistoryCalendar` + `MonthTileState` for tile state; the existing
  `tr-note--signal` pattern on Home.
- _Harden:_ drift is only honest if the current draft reflects the user's route choices — today
  those live in a page-local dictionary (see proposal 2). Without 2, compare locations + km from
  default routes and label route-only differences as "route changed" rather than drift.
- _Improve perf:_ History would rebuild 12 drafts per year view; each is one range query + pure
  compute, so fine — but batch it as one attendance range query for the year rather than 12.
- _New surface:_ pure `DeclarationDrift.Compare(IReadOnlyList<DeclarationRow>, DeclarationDraft)`
  → `DriftReport(changedDays, kmDelta)` in Core (unit-testable), a `MonthTileState.Stale` variant,
  and three small UI touchpoints. No data model change.

**Effort:** M  **Risk:** low (read-only)
**Open questions:** compare against the *latest* run only (my recommendation), or any run? Should
archived-location renames count as drift? (Frozen `LocationName` vs current name — probably not;
compare by id.)

### 2. Remember route choices per day

**The gap.** `Declaration.razor` keeps overrides in `_routeOverride`, a page-local
`Dictionary<DateOnly, int?>`, and clears it on every month change. Leave the page or navigate a
month and your picks are gone. This also produces **two different totals for the same month**:
`/attendance`'s counter uses `TravelKmCalculator` (default routes only) while `/declaration`'s
counter uses the overrides. Location edits in the grid are persisted through
`IAttendanceStore.SetAsync`; route edits are not — an asymmetry.

**What it adds.** Pick "via A12 (roadworks)" once for a day and it sticks: across navigation,
across sessions, into the attendance counter, and into drift detection. Re-declaring a month
reproduces the previous run exactly unless you change something.

**Fit & architecture**
- _Reuse:_ `AttendanceEntry` is already the per-day system of record; `DeclarationBuilder.ApplySelections`
  already knows how to overlay per-day route selections — it just needs them to come from the store
  instead of the page.
- _Harden:_ route delete is a hard delete (ARCHITECTURE §10) — a stored `RouteId` must degrade to
  "use the default" (or `HasNoRoute`) when the route vanishes, with a test. `TravelKmCalculator`
  gains the per-day route so both counters agree.
- _Improve perf:_ none needed.
- _New surface:_ nullable `AttendanceEntry.RouteId` (migration), `IAttendanceStore.SetRouteAsync`,
  `BuildDraftAsync` reads it. `GenerationService`'s `routeSelections` parameter can then go away.

**Effort:** M  **Risk:** med — data model change on the only copy of attendance history (additive
nullable column, so migration is safe, but it's the careful zone).
**Open questions:** does clearing a day's mark also clear its route? (Yes.) Does changing a day's
location clear it? (Yes — matches today's `_routeOverride.Remove(date)`.)

### 3. Rhythm suggestions — ghost marks from your own pattern

**The gap.** Most hybrid weeks repeat ("office Tue + Thu"). The brush model makes painting fast,
but you still paint the same shape every week, and forgotten days silently become Home. The data
to predict the week is already in `AttendanceEntry` history — the app never looks back at it.

**What it adds.** On `/attendance`, unmarked weekdays in the visible month show a faint ghost
bullet of the likely location (e.g. 80% of the last 8 Tuesdays were Office). One "Accept
suggestions" button — or a click on a ghost — turns them into real marks. On `/declaration`,
unmarked-default rows that the rhythm says were probably Office get a hint ("usually Office on
Tuesdays"), which is exactly where a forgotten office day costs real money.

**Fit & architecture**
- _Reuse:_ `IAttendanceStore.GetRangeAsync`, `BrushResolver`/`ApplyOutcome` for accepting,
  existing `tr-bullet--outline` styling for the ghost look, `MonthGridBuilder`.
- _Harden:_ `ContrastText`/`TryParseHex`/`Initial` are copy-pasted in `Attendance.razor`,
  `Declaration.razor` and `Home.razor`; a ghost bullet would be a fourth copy — extract a shared
  `LocationBullet` component first.
- _Improve perf:_ one extra range query (last ~8–12 weeks) per month load; trivial.
- _New surface:_ pure `RhythmSuggester.Suggest(history, targetDates, minConfidence)` →
  `Dictionary<DateOnly, (locationId, confidence)>` in Core, with tests. Suggestions are **never
  written without a click** — nothing silently changes the system of record.

**Effort:** M  **Risk:** low (suggest-only)
**Open questions:** suggest for future days too (planning) or only past/today? Confidence
threshold — start at ≥ 60% over ≥ 4 samples?

### 4. Near-zero-click capture from the reminder toast

**The gap.** The reminder already knows today is unlogged and already runs in the user's desktop
session (`Remind-Transit.ps1`), but its only action is "Log now", which opens the browser. The
toast is the moment of highest intent and the lowest-friction surface you have, and it can't
actually record anything. The desktop helper also has a signal the IIS app cannot see: the
**network you're connected to** (`Get-NetConnectionProfile` / Wi-Fi SSID). On the office network
means you're almost certainly at the office.

**What it adds.** The toast says *"You're on TRUSTON-CORP — log today as Office?"* with buttons
**Office** / **Home** / Open. One click, done, no browser. Without a network hint it falls back to
the rhythm guess from proposal 3, then to plain buttons for the top two locations.

**Fit & architecture**
- _Reuse:_ `ReminderEndpoints` (loopback, already mapped), `IAttendanceStore.SetAsync`,
  `AttendanceStatus`, the installer's Scheduled Task and BurntToast wiring.
- _Harden:_ a write endpoint goes against the "no network API" rule in CLAUDE.md. Keep it
  narrow: `POST /api/reminder/log-today` that only accepts *today*, only when today is unlogged
  (never overwrites an explicit mark), and only from loopback. BurntToast buttons with
  `-ActivationType Protocol` open a URL, so either register a tiny `transit:` protocol handler
  (HKCU, from the installer) that POSTs silently, or use a background-activation script. The
  handler is the fiddly part.
- _Improve perf:_ none.
- _New surface:_ optional `WorkLocation.NetworkHint` (string, matched against connection profile
  names) + one editor field; the endpoint also returns the suggestion so the script stays dumb.

**Effort:** M  **Risk:** med — changes a careful-zone rule (write API, even loopback-only) and adds
a data model field. Needs your explicit sign-off.
**Open questions:** is a loopback write endpoint acceptable at all? If not, a
`/attendance/quick?loc=…` page that marks and closes gives most of the value with no API change.
Auto-log on network match (with an "undo" toast) versus always confirm — I'd start with confirm.

### 5. Pre-flight check before Generate

**The gap.** `Generate` is only disabled when there's no active profile. It happily produces a
declaration with travel days that have **no route (declared at 0 km)**, for a month that isn't
finished yet (remaining weekdays silently default to Home), or for a month already declared
(creating run #n with no indication of what changed). The grid flags no-route rows, but a
flagged row in a 22-row grid is easy to scroll past, and the output is a document you sign.

**What it adds.** Clicking Generate opens a short checklist dialog only when something is off:
"2 travel days have no route (0 km)", "6 weekdays after today will be declared as Home",
"September is already declared — 1 day differs from the last run" (from proposal 1). Clean months
generate straight through, as now.

**Fit & architecture**
- _Reuse:_ `DeclarationDraft` already carries `HasNoRoute`/`IsUnmarkedDefault`; the Home page
  already computes `_noRoute`/`_unmarked` — move that into Core instead of duplicating it again.
- _Harden:_ this is the harden-half of the feature: generation is append-only, so a bad run can
  be deleted but not fixed; catching problems before the run is cheaper.
- _Improve perf:_ none.
- _New surface:_ pure `GenerationReadiness.Check(draft, today, latestRun?)` → list of issues
  (Core, tested); one `MudDialog` on `/declaration`. Home's metrics can use the same function.

**Effort:** S  **Risk:** low
**Open questions:** should "no route" block generation, or only warn? (I'd warn.)

---

## Backlog

- **Estimated reimbursement in €** — a `RatePerKm` option (e.g. €0.23) shown next to
  `TravelKmCounter` and the History year total; display only, the sheet still owns totals.
  (S, needs an `IOptions` setting.)
- **Keyboard painting** — digits pick a brush, arrows move a day cursor, Space paints; the
  calendar is pointer-only today (`Attendance.razor` `@onpointerdown/enter`). Speed plus
  accessibility. (S)
- **Public-holiday awareness** — pure Dutch holiday calculator (Easter-based, no dependency);
  mute holidays on the calendar, exclude them from "unmarked" counts, skip the reminder on them.
  (S)
- **Attach the signed PDF back to its run** — `ArtifactKind` has only `Xlsx`/`Pdf`; add
  `SignedPdf` so the archive holds what was actually submitted. (M, data model)
- **"Submitted" marker on a run** — record the date you sent it; Home's nudge can tell
  "generated, not sent" from "done". (S–M, data model)
- **Template auto-mapping** — on upload, scan headers (`datum`, `van`, `naar`, `aantal km`) via
  `ISheetPreviewReader` and propose `ColumnMapping`s + `DataStartRow`. Mostly helps after an
  employer template change. (M)
- **Year export** — all latest-run frozen rows for a year in one sheet (ClosedXML already there),
  handy for tax or disputes. (S)
- **Copy last week** — one button on `/attendance` to repeat the previous week's marks onto the
  current week; a simpler, deterministic cousin of proposal 3. (S)

---

## Deliberately not proposed

- **Outlook/Teams calendar sync to infer location** — needs Graph auth and a network dependency;
  breaks the no-network, no-secrets rule.
- **Mobile capture / PWA** — explicit non-goal, and the site is loopback-only.
- **GPS or geofencing** — no location hardware story on a work laptop; the network hint in
  proposal 4 gets you the same signal locally.
- **Multi-leg days (home → office → client)** — would rework the route model and the one-row-per-day
  template layout, and there's no evidence it's needed.
- **Charts/analytics dashboard** — the Home breakdown already answers the real question; more
  charts would be decoration.
