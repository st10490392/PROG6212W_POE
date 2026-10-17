# Part 2 final local acceptance report

Verified on 17 October 2026 for `st10490392/PROG6212W_POE`, branch `feature/part2-raceday-api`.
Initial commit: `7a0537e`. Verified implementation/fix commit: `bda6c7a`.

Local checks were executed through this session's Windows tools against `.\SQLEXPRESS` and `https://localhost:7142`. GitHub-hosted SQLite/LocalDB verification is identified separately below. The initial working tree was clean; existing history and Part 1 artifacts were preserved. No merge into main or history rewrite was performed.

## Final outcome

| Verification | Actual outcome |
| --- | --- |
| Local SQL Server Express, complete updated suite | 109 passed, 0 failed, 0 skipped; 109 total |
| Local portable suite before the two precision regression cases were added | 105 passed, 0 failed, 2 SQL-only skipped; 107 total |
| GitHub Ubuntu / SQLite, updated implementation | 107 passed, 0 failed, 2 SQL-only skipped; 109 total |
| GitHub Windows / SQL Server LocalDB, updated implementation | 109 passed, 0 failed, 0 skipped |
| Final real HTTPS scenario | All 44 recorded requests and data-integrity checks passed |
| Final demo cleanup | verified: removed 3 synthetic users and their demo event data |
| Post-suite SQL database cleanup | 0 remaining `RaceDayTests_*` databases, confirmed by a separate master-database query |

The local Express suite completed in **20 minutes 53 seconds** using serial test collections. TRX results are retained locally under `TestResults/AcceptanceSqlServer` and `TestResults/AcceptanceSqlite` (ignored by Git).
The SQL TRX was inspected to confirm that both SQL-only facts, the complete acceptance scenario, and both timestamp rounding regression cases actually passed.

## SQL connection and applied migration

- Repository-local .NET SDK: **10.0.402**, runtime **10.0.13**, Windows x64. The tool shell initially lacked dotnet on PATH; the existing ignored SDK was used.
- `sc.exe query 'MSSQL$SQLEXPRESS'` returned **STATE 4 RUNNING**.
- A read from `RaceDayPart2.__EFMigrationsHistory` returned **20261017091246_InitialCreate**, product version **10.0.13**. This matches the single source migration.
- The timestamp response fix changes controller behaviour and requires **no new schema migration**.

Initial SQL reads failed during encrypted pre-login/login timeouts, including an explicit shared-memory retry. A later read using 120-second login/query timeouts succeeded. Initial HTTP probes also timed out without receiving an HTTP status.

Windows reported approximately 2 GB RAM with roughly 200 MB free during the slow build. One SQL-suite attempt was stopped during compilation without test totals and is not counted as passing or completed. The retry used one build worker, disabled reusable build servers, and serial test collections. Memory pressure is a plausible contributor to the delays; its exact cause was not isolated.

## Failure found and corrected

The first scripted HTTPS trial returned Swagger **200**, then timed out on event types without an HTTP response; it exited 1 before creating accounts.
Evidence remains locally in `TestResults/acceptance-http.json`.

The next trial exercised registration, login, event/category creation, enrolment and ownership denials, then failed the create/read consistency check:
the create response contained `2026-10-19T14:38:25.815528Z`, while SQL Server's `datetime2(0)` stored and returned `2026-10-19T14:38:26Z`.
This was a response precision mismatch, not an unauthorised event mutation. All three synthetic users and their demo data were removed in a verified SQL transaction.
Evidence remains locally in `TestResults/acceptance-http-retry.json`.

`EventsController` now reloads saved scalar values before returning create/update responses.
Two new regression cases cover fractional seconds that SQL rounds down and up, comparing POST/PUT responses with actual GET readback.
The final SQL suite and the final live retry passed after rebuilding and restarting the API.

The new complete acceptance integration test additionally proves that forbidden event/category/result writes leave persisted data unchanged.
It uses TestServer with isolated relational databases; the separate live runner below uses actual HTTPS and Kestrel.

## Final real HTTPS evidence

Run window: **2026-10-17T15:20:09.558429+00:00** to **2026-10-17T15:21:01.486238+00:00**.
Full sanitized responses are preserved in [ACCEPTANCE_HTTP.json](ACCEPTANCE_HTTP.json), copied from the actual final runner output.
Passwords, cookie values, profile/contact fields, emails and trace IDs are omitted. Identifiers and event names belong only to freshly generated demo accounts/data.
A null response denotes an empty body or omitted Swagger HTML.

| Operation | Actual request | HTTP |
| --- | --- | --- |
| Swagger UI | `GET /swagger/index.html` | 200 |
| event types | `GET /api/event-types` | 200 |
| register Organiser | `POST /api/auth/register` | 201 |
| registration does not authenticate | `GET /api/users/me` | 401 |
| login Organiser | `POST /api/auth/login` | 200 |
| register Organiser | `POST /api/auth/register` | 201 |
| registration does not authenticate | `GET /api/users/me` | 401 |
| login Organiser | `POST /api/auth/login` | 200 |
| register Participant | `POST /api/auth/register` | 201 |
| registration does not authenticate | `GET /api/users/me` | 401 |
| login Participant | `POST /api/auth/login` | 200 |
| invalid credentials | `POST /api/auth/login` | 401 |
| invalid credentials do not authenticate | `GET /api/users/me` | 401 |
| create event | `POST /api/events` | 201 |
| create Distance category | `POST /api/events/2/categories` | 201 |
| create Age category | `POST /api/events/2/categories` | 201 |
| enrol participant | `POST /api/events/2/enrolments` | 201 |
| duplicate enrolment | `POST /api/events/2/enrolments` | 409 |
| participant cannot modify event | `PUT /api/events/2` | 403 |
| participant cannot delete event | `DELETE /api/events/2` | 403 |
| other organiser cannot modify event | `PUT /api/events/2` | 403 |
| other organiser cannot delete event | `DELETE /api/events/2` | 403 |
| other organiser cannot modify category | `PUT /api/categories/3` | 403 |
| other organiser cannot delete unused category | `DELETE /api/categories/4` | 403 |
| other organiser cannot read enrolments | `GET /api/events/2/enrolments` | 403 |
| other organiser cannot read results | `GET /api/events/2/results` | 403 |
| event unchanged after denied writes | `GET /api/events/2` | 200 |
| owner reads enrolments | `GET /api/events/2/enrolments` | 200 |
| result before start | `POST /api/enrolments/2/result` | 409 |
| move demo event start into past | `PUT /api/events/2` | 200 |
| participant cannot record result | `POST /api/enrolments/2/result` | 403 |
| other organiser cannot record result | `POST /api/enrolments/2/result` | 403 |
| owner records result | `POST /api/enrolments/2/result` | 201 |
| duplicate result | `POST /api/enrolments/2/result` | 409 |
| other organiser cannot correct result | `PUT /api/results/1` | 403 |
| result unchanged after denied correction | `GET /api/events/2/results` | 200 |
| owner corrects result | `PUT /api/results/1` | 200 |
| participant reads corrected history | `GET /api/users/me/results` | 200 |
| logout | `POST /api/auth/logout` | 204 |
| protected profile after logout | `GET /api/users/me` | 401 |
| logout | `POST /api/auth/logout` | 204 |
| protected profile after logout | `GET /api/users/me` | 401 |
| logout | `POST /api/auth/logout` | 204 |
| protected profile after logout | `GET /api/users/me` | 401 |

Selected actual sanitized responses:

```text
invalid credentials: HTTP 401 {"title":"Invalid email or password.","status":401}
participant cannot modify event: HTTP 403 null
other organiser cannot read enrolments: HTTP 403 {"title":"This event belongs to another organiser.","status":403}
result before start: HTTP 409 {"title":"Results cannot be recorded before the event starts.","status":409}
owner records result: HTTP 201 {"resultId":1,"enrolmentId":2,"participantId":6,"eventId":2,"eventDate":"2026-10-16T15:20:57Z","categoryId":3,"finishTime":"00:52:18","finishingPosition":47}
other organiser cannot correct result: HTTP 403 {"title":"This event belongs to another organiser.","status":403}
owner corrects result: HTTP 200 {"resultId":1,"enrolmentId":2,"participantId":6,"eventId":2,"eventDate":"2026-10-16T15:20:57Z","categoryId":3,"finishTime":"00:51:59","finishingPosition":46}
participant reads corrected history: HTTP 200 [{"resultId":1,"enrolmentId":2,"participantId":6,"eventId":2,"eventDate":"2026-10-16T15:20:57Z","categoryId":3,"finishTime":"00:51:59","finishingPosition":46}]
```

The owner moved only the synthetic event start into the past after enrolment to exercise result capture.
The underlying contract has a start date but no end date; the organiser remains responsible for confirming the event has finished.
Duplicate enrolment/result conflicts, denied ownership operations, invalid credentials and post-logout requests returned the expected 409/403/401 codes.
Re-reads verified unchanged event/category/result data after denied writes and the corrected finish time/position in Participant history.

## Repeatable commands from part2

Use the installed SDK on PATH, or configure the existing workspace-local SDK:

```powershell
$env:DOTNET_ROOT = (Resolve-Path ../.tools/dotnet).Path
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet --info
sc.exe query 'MSSQL$SQLEXPRESS'
sqlcmd -S '.\SQLEXPRESS' -E -C -d RaceDayPart2 -b -l 120 -t 120 -Q 'SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory;'
dotnet restore RaceDay.slnx
```

To reproduce the serial settings used for this machine, create an equivalent local runsettings file:

```powershell
New-Item -ItemType Directory -Force TestResults | Out-Null
'<RunSettings><RunConfiguration><MaxCpuCount>1</MaxCpuCount></RunConfiguration><xUnit><MaxParallelThreads>1</MaxParallelThreads><ParallelizeTestCollections>false</ParallelizeTestCollections></xUnit></RunSettings>' | Set-Content TestResults/serial.runsettings
$env:RACEDAY_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test RaceDay.slnx --no-restore --configuration Release --disable-build-servers -m:1 -p:UseSharedCompilation=false --settings TestResults/serial.runsettings --logger trx --results-directory TestResults/AcceptanceSqlServer
dotnet test RaceDay.slnx --no-build --configuration Release --filter FullyQualifiedName~AcceptanceTests --logger 'console;verbosity=detailed' --logger trx --results-directory TestResults/AcceptanceScenario
Remove-Item Env:RACEDAY_TEST_SQLSERVER
```

The actual local invocation used the same XML in ignored `../.tools/acceptance.runsettings`.
An unset SQL variable selects SQLite; an unavailable configured SQL server fails rather than silently falling back.
SQL tests migrate and remove only their own randomly named `RaceDayTests_<guid>` databases.

Start the updated API using the README launch instructions, then run the optional live checker (Python 3.10+ standard library and sqlcmd on PATH):

```powershell
python scripts/acceptance_http.py --output TestResults/acceptance-http-final.json
```

This is the final live command executed. Defaults are HTTPS localhost:7142 and the local Express `RaceDayPart2` database.
Use the database actually configured for the API. The runner permits only HTTPS loopback origins/local SQL instances, disables redirects, accepts the local development certificate for that process, and keeps cookies/passwords in memory.
Its SQL cleanup verifies exact fresh email/ID pairs, rejects unexpected event ownership, and deletes only that run's demo data in a transaction. Cleanup failures produce a nonzero exit.
Python is optional verification tooling; the C# application and dotnet test suite do not require it.

The updated Release API was left running in a hidden background process on HTTPS 7142 / HTTP 5142.
Its PID and diagnostic logs are in ignored `../.tools/acceptance-api.pid` and `acceptance-api.stdout.log` / `acceptance-api.stderr.log`.
The original development process was stopped only after verifying its executable belonged to this repository.
Restarting the API clears in-memory sessions as documented. PowerShell script policy was not changed.

## GitHub Actions and commit evidence

[Passing updated implementation run 39042245274](https://github.com/st10490392/PROG6212W_POE/actions/runs/39042245274) verified `bda6c7a`.
Both Release builds reported **0 warnings and 0 errors**, and both focused sanitized acceptance transcript runs passed.
Both full-suite and focused TRX artifacts are retained by the workflow.
[Part 2 workflow runs](https://github.com/st10490392/PROG6212W_POE/actions/workflows/part2-api.yml) show actual status for subsequent documentation/runner commits.

Meaningful history was inspected with `git log --oneline origin/main..HEAD`.
Existing commits separately implement authentication, profiles, events, categories, enrolments, results, database constraints/migrations and CI.
This verification adds genuine changes:

| Commit | Improvement |
| --- | --- |
| `0e4059b` | Complete repeatable acceptance scenario, sanitized HTTP output and denied-write integrity checks |
| `f6e9b45` | CI acceptance transcripts on both providers and retention of nested TRX artifacts |
| `bda6c7a` | Actual SQL timestamp response bug fix and two rounding regression cases |

The final documentation/runner/evidence commit follows these on the same Part 2 branch. No empty evidence commits or merge into main were used.
Commits disclose OpenAI Codex assistance; a per-command Codex identity was used without changing the user's Git configuration.

## Outstanding personal evidence and limitations

- The student must supply real Swagger screenshots, real SSMS migration/table/relationship screenshots, and real passing test/Actions screenshots for the submitted revision.
- The student must record their own narrated demonstration, explain the C# implementation and tests independently, and add the actual unlisted video URL.
- README placeholders identify these items. No screenshot, narration or video URL was fabricated.
- Initial startup/connection delays were observed and are retained above; their exact environmental cause remains undiagnosed despite passing final runs.
- The API is a single-instance academic application with in-memory sessions and start-date-based result timing, as documented in CONTRACT_NOTES.md.

No remaining functional acceptance failures were observed in the final local run.
