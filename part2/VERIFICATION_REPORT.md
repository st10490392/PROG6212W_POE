# RaceDay Part 2 verification record

This is an in-progress record. Build, test and CI results below must be replaced with observed outcomes before claiming completion.

## Scope and preservation

- Development branch: `feature/part2-raceday-api`.
- Starting commit: `efd283185d9fbdf3a7fd1097b7bdebf51e29a37b`.
- Original main reference: `89b757c92df5d27e4fc49ef1ad6a5dc5dde046a7`.
- All 24 planned method/route combinations are implemented; see `ENDPOINT_COVERAGE.md`.
- Part 1 database script, endpoint plan, ERD, commit log and workflow have not been edited.
- No deployment, merge, Part 3 frontend, Docker or Azure Blob Storage implementation has been performed.

## Observed checks so far

| Check | Observed outcome |
|---|---|
| Source inspection | Part 1 README, SQL, endpoint plan, implementation plan, existing workflow and commit history read |
| Static contract/configuration check | Passed: 24 exact method/route combinations; project XML and configuration JSON parse |
| First dependency restore | Failed: NU1900 DNS failure retrieving NuGet vulnerability data |
| Audited retry with initial dependencies | Failed: advisories in transitive OpenAPI, SQLite and PKCS packages |
| Patched dependency restore | API and test projects restored successfully with auditing enabled |
| API Release compilation | Passed: 0 warnings, 0 errors |
| Full solution Release compilation | Running; test project compilation not yet verified |
| Automated unit/HTTP/database tests | Not yet executed |
| Local SQL Server Express service | Start denied without administrator rights; Windows administrator prompt was cancelled |
| GitHub Actions | Workflow configured; no pushed Part 2 run verified yet |

The optional cached-restore PowerShell workaround was blocked by Windows script policy and did not run. NuGet vulnerability auditing remains enabled in project/CI settings.

## Files and implementation

- `.gitignore`: exclude SDK, build outputs, results and local secrets.
- `part2/RaceDay.slnx`, `Directory.Build.props`, `global.json`, `.config/dotnet-tools.json`: solution and SDK/tool settings.
- `part2/RaceDay.Api`: controllers, request DTOs, safe responses, six models, EF mapping, server ticket store, password hashing, ownership service, transactional writes, origin checks and error handling.
- `part2/RaceDay.Tests`: isolated SQLite/SQL Server factory, controllable clock, validation/session unit tests and HTTP/database integration tests.
- `.github/workflows/part2-api.yml`: restore/build/test jobs for SQLite and SQL Server LocalDB, with retained TRX evidence.
- `README.md`, `part2/CONTRACT_NOTES.md`, `ENDPOINT_COVERAGE.md`, `PRESENTATION_GUIDE.md`: setup, documented decisions, route/test map and presentation examples.
- The submitted application and automated tests are C#. Temporary agent tooling is excluded from Git; setup uses the standard .NET CLI.

## Student actions and known specification limits

Follow the root README to start SQL Server, restore the EF tool, apply the migration to an empty `RaceDayPart2` database, trust the HTTPS development certificate, launch Swagger and run tests. Never run the destructive Part 1 SQL script over the Part 2 EF database.

The referenced coursework PDF is not present in the supplied repository; the unseen full marking rubric has not been independently checked. An event has a start date but no finish date/publish flag, so result capture can reject pre-start events but the organiser must confirm the race has finished. Server sessions are held in memory for this single-instance academic API and expire on restart.

The student still needs to review and understand the AI-assisted code, capture real screenshots and record the actual presentation video. Evidence placeholders are intentional; no screenshots or video URLs are invented.
