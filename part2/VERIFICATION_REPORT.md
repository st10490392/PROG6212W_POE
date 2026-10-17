# RaceDay Part 2 verification record

Verified on 17 October 2026 against C# implementation commit `2bb1dde8d25e4a87eb9f96c8dc620c781ce2e4dc`. Later evidence/documentation commits retain the same application and test source. Results below come from actual command output and GitHub Actions job logs.

## Scope and preservation

- Development branch: `feature/part2-raceday-api`.
- Starting commit: `efd283185d9fbdf3a7fd1097b7bdebf51e29a37b`.
- Original main reference: `89b757c92df5d27e4fc49ef1ad6a5dc5dde046a7`.
- Local `main`, `origin/main` and GitHub's `main` still have that original SHA.
- 27 meaningful implementation/test/CI/documentation commits exist after the starting commit at the verified implementation SHA. Subsequent evidence commits are additional; AI assistance is disclosed in commit messages and the README.
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
| Full solution Release compilation | Passed locally and in both CI jobs: 0 warnings, 0 errors |
| EF migration/model consistency | Passed: `dotnet ef migrations has-pending-model-changes` reported no changes since the last migration |
| Migration SQL export | Passed: generated and reviewed `InitialCreate.sql`; not executed against the student's database |
| SQLite suite in Ubuntu CI | Passed: 104 passed, 0 failed, 2 SQL-only tests skipped, 106 total |
| Full suite against real SQL Server LocalDB in Windows CI | Passed: 106 passed, 0 failed, 0 skipped |
| Local SQLite rerun | Passed: 104 passed, 0 failed, 2 SQL-only tests skipped, 106 total; reported test duration 2m31s |
| Local SQL Server Express service | Start denied without administrator rights; Windows administrator prompt was cancelled |
| GitHub Actions | Both jobs passed in [run 39021185258](https://github.com/st10490392/PROG6212W_POE/actions/runs/39021185258) at the verified implementation SHA; TRX artifacts retained |
| Part 1 preservation | Passed: `git diff --exit-code` against the starting commit for `docs` and the Part 1 workflow |
| Whitespace validation | Passed: `git diff --check` |

The optional cached-restore PowerShell workaround was blocked by Windows script policy and did not run. NuGet vulnerability auditing remains enabled in project/CI settings.

Local passing TRX: `part2/TestResults/Ginger_DESKTOP-6JKPP9Q_2026-10-17_11_43_40_net10.0.trx` (ignored build evidence, not committed). CI retains its separate portable and SQL Server TRX artifacts on the linked run.

The first local run found 57 failures, 46 passes and 2 skips. Separating event time from authentication time and using the cookie handler's configured lifetime corrected the shared expiry issue. The next CI run exposed two remaining SQLite failures: re-login renewed a revoked session key, and SQLite's text decimal comparison accepted zero distance. A fresh sign-in handler after revocation and numeric SQLite check expressions fixed those defects. Regression coverage also verifies the replacement session works and expires after 30 minutes. The final SQL Server job used the real migration and native SQL types; compatibility is not inferred from SQLite.

## Files and implementation

- `.gitignore`: exclude SDK, build outputs, results and local secrets.
- `part2/RaceDay.slnx`, `Directory.Build.props`, `global.json`, `.config/dotnet-tools.json`: solution and SDK/tool settings.
- `part2/RaceDay.Api`: controllers, request DTOs, safe responses, six models, EF mapping, server ticket store, password hashing, ownership service, transactional writes, origin checks and error handling.
- `part2/RaceDay.Api/Data/Migrations`: generated C# initial migration, designer, model snapshot and reviewable SQL export for the six-table SQL Server schema.
- `part2/RaceDay.Tests`: isolated SQLite/SQL Server factory, controllable clock, validation/session unit tests and HTTP/database integration tests.
- `.github/workflows/part2-api.yml`: restore/build/test jobs for SQLite and SQL Server LocalDB, with retained TRX evidence.
- `README.md`, `part2/CONTRACT_NOTES.md`, `ENDPOINT_COVERAGE.md`, `PRESENTATION_GUIDE.md`: setup, documented decisions, route/test map and presentation examples.
- The submitted application and automated tests are C#. Temporary agent tooling is excluded from Git; setup uses the standard .NET CLI.

For the complete file list, run `git diff --name-only efd283185d9fbdf3a7fd1097b7bdebf51e29a37b..HEAD`. All 24 implemented methods, routes and covering test classes are listed in `ENDPOINT_COVERAGE.md`.

## Commands and presentation handover

Start the Express service from an administrator PowerShell if needed: `Start-Service -Name 'MSSQL$SQLEXPRESS'`. Add the local SDK to PATH as shown in the README if `dotnet` is unavailable. From `part2`:

```powershell
dotnet restore RaceDay.slnx
dotnet tool restore
dotnet build RaceDay.slnx --configuration Release
dotnet ef database update --project RaceDay.Api
dotnet dev-certs https --trust
dotnet run --project RaceDay.Api --launch-profile https
```

Open `https://localhost:7142/swagger`. In a separate terminal, run `dotnet test RaceDay.slnx --configuration Release --logger trx --results-directory TestResults`. See the README for `RACEDAY_TEST_SQLSERVER` when repeating the entire suite against your own SQL Server.

Explain registration/password hashing and server session revocation using `AuthController` and `ServerTicketStore`; show how the six models and `RaceDayDbContext` produce the migration; demonstrate role attributes followed by `AccessService` ownership checks; trace a DTO-validated request through a controller to `SaveChangesAsync`; distinguish small validation/session unit tests from the real HTTP and relational integration tests. `PRESENTATION_GUIDE.md` provides the full demonstration sequence and concrete examples.

## Student actions and known specification limits

The C# API implementation and automated verification are complete. The remaining local demonstration prerequisite is starting SQL Server Express and applying the migration; the administrator prompt was cancelled, so no successful local SQL Server run or manual database-backed Swagger demonstration is claimed. Real SQL Server verification was performed successfully in GitHub Actions.

Follow the root README to start SQL Server, restore the EF tool, apply the migration to an empty `RaceDayPart2` database, trust the HTTPS development certificate, launch Swagger and run tests. Never run the destructive Part 1 SQL script over the Part 2 EF database.

The referenced coursework PDF is not present in the supplied repository; the unseen full marking rubric has not been independently checked. An event has a start date but no finish date/publish flag, so result capture can reject pre-start events but the organiser must confirm the race has finished. Server sessions are held in memory for this single-instance academic API and expire on restart.

The student still needs to review and understand the AI-assisted code, capture real screenshots and record the actual presentation video. Evidence placeholders are intentional; no screenshots or video URLs are invented.
