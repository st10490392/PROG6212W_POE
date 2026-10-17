# RaceDay – Programming 2B Portfolio of Evidence

RaceDay is an event-management platform for South African road running, walking, and cycling events. This repository preserves the Part 1 planning artifacts and implements the Part 2 REST API in C#, ASP.NET Core, Entity Framework Core Code-First and SQL Server. The Part 3 MVC frontend is outside this implementation.

## Roles

- Organiser: creates, edits, and deletes events; manages categories; views event enrolments; captures results.
- Participant: creates an account; browses events; selects a category and enrols; views personal enrolments and results.

## Part 1 deliverables

The required files are stored in the docs folder:

- `RaceDay_ERD.png` – Entity Relationship Diagram showing entities, keys, relationships, and cardinality.
- `API_Endpoint_Plan.md` – planned RESTful API routes for authentication, profile, events, categories, enrolments, and results.
- `RaceDay_Database.sql` – SQL Server schema with constraints and seeded sample data.

## Database design

The ERD includes the planned domain entities for the RaceDay system, including Users, EventTypes, Events, Categories, Enrolments, and Results. The SQL script is structured to align with that design and includes primary keys, foreign keys, uniqueness constraints, and validation rules.

## Running the SQL script

1. Open SQL Server Management Studio (SSMS).
2. Open `docs/RaceDay_Database.sql`.
3. Execute the script against a clean SQL Server instance.
4. Confirm that `RaceDayDB` is created and the seed queries return the sample organiser, participant, event, category, enrolment, and result records.

The demo password values in the seed data are placeholders for Part 1 planning only. In Part 2, the application should store actual ASP.NET-compatible password hashes.

## GitHub / CI

The repository includes a GitHub Actions workflow in `.github/workflows/part1-validate.yml` to validate the existence of the required Part 1 artifacts.

CI screenshot: Add your successful GitHub Actions green build screenshot here.

## Video presentation

YouTube link: Add your unlisted YouTube presentation link here.

## Student details

- Student name: RIPFUMELO VUNENE NGOBENI
- Student number: ST10490392
- Module: PROG6212 Programming 2B
- Assessment: Part 1 – System Planning and Database
- Current development: Part 2 – RESTful API, on `feature/part2-raceday-api`

## AI disclosure

AI tools may be used for planning, proofreading, or coding support only if the use is disclosed in line with the assessment rules. Any AI-generated content must be reviewed, understood, and adapted before submission, and must not be submitted as the student’s own work without their own analysis and critical thinking.

---

## Project status update — 13 October 2026

RaceDay is academic event-management coursework. The documented ERD, SQL Server schema and API plan are planning deliverables; no production service or October 2026 launch is claimed.

## Part 2 project structure

- `part2/RaceDay.Api/Controllers`: the 24 operations in the [Part 1 endpoint contract](docs/API_Endpoint_Plan.md).
- `Models`: Users, EventTypes, Events, Categories, Enrolments and Results.
- `DTOs`: validated input contracts and safe response projections.
- `Data`: SQL Server EF model configuration and Code-First migrations.
- `Security`: server-side authentication tickets and password timing support.
- `Services`: shared identity, ownership and category-membership checks.
- `Infrastructure`: safe errors, origin checks and transactional writes.
- `part2/RaceDay.Tests`: unit tests and HTTP/relational integration tests.
- [Contract notes](part2/CONTRACT_NOTES.md): UTC dates, filters, business rules and documented specification gaps.

## Requirements and installation

Install Git, the **.NET 10 SDK**, and **SQL Server Express, Developer edition or LocalDB**. SSMS is useful for inspecting the database. No SQL password is needed with Windows integrated authentication.

```powershell
git clone https://github.com/st10490392/PROG6212W_POE.git
cd PROG6212W_POE
git switch feature/part2-raceday-api
cd part2
dotnet restore RaceDay.slnx
dotnet tool restore
dotnet build RaceDay.slnx --configuration Release
```

On this workspace, a local .NET SDK is already installed in the ignored `.tools/dotnet` folder. From the repository root, add it to the current PowerShell session if `dotnet` is not on PATH:

```powershell
$env:DOTNET_ROOT = (Resolve-Path .tools/dotnet).Path
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
```

For a fresh clone, install the .NET 10 SDK using Microsoft's standard installer. Tool/package restores need internet access. The RaceDay API and all automated tests are C#; no Python installation is required to build, run or test the project.

## SQL Server and EF Core setup

Start the SQL Server instance first. For this computer's Express instance, run the following in an **administrator PowerShell** if it is stopped:

```powershell
Start-Service -Name 'MSSQL$SQLEXPRESS'
```

The development default connection is `Server=.\SQLEXPRESS;Database=RaceDayPart2;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True`. It contains no password. `TrustServerCertificate=True` is for the local development instance; use a trusted SQL Server certificate for other environments.

From `part2`, configure a different instance with user secrets (kept outside the repository):

```powershell
dotnet user-secrets set 'ConnectionStrings:RaceDay' 'Server=(localdb)\MSSQLLocalDB;Database=RaceDayPart2;Integrated Security=True;Encrypt=True;TrustServerCertificate=True' --project RaceDay.Api
dotnet ef database update --project RaceDay.Api
```

For Express, keep the default or set the secret to the Express connection string. Deployment environments can use the `ConnectionStrings__RaceDay` environment variable. Never commit SQL credentials, exported secrets or real personal data.

EF migrations create a separate **RaceDayPart2** database with the six tables, constraints and Run/Walk/Cycle seeds. Register your own demonstration accounts through the API; no passwords or fake Part 1 hashes are seeded. The API does not silently migrate a database at startup. Inspect migrations before applying them.

Keep the Part 1 SQL script for the Part 1 demonstration. It drops/recreates its tables and must **not** be executed over the Part 2 EF database. Do not point EF at an existing Part 1 `RaceDayDB` and assume migration history exists.

## Launch and Swagger

From `part2`:

```powershell
dotnet dev-certs https --trust
dotnet run --project RaceDay.Api --launch-profile https
```

Open **https://localhost:7142/swagger**. Swagger is enabled in Development; use HTTPS because the session cookie is Secure.

1. Register an Organiser through `POST /api/auth/register`. Example JSON:

   ```json
   { "firstName": "Lerato", "lastName": "Mokoena", "email": "organiser@raceday.test", "password": "Choose-your-own-12-plus-character-password", "role": "Organiser" }
   ```

2. Log in using `POST /api/auth/login` with your email and password. Registration itself does not authenticate you.
3. Swagger shares the API origin, so the browser sends the HttpOnly session cookie automatically. No bearer token or manual cookie entry is needed.
4. Create an event with `eventTypeId: 1`, positive `distanceKm`, and an explicit-offset future date such as `2030-06-02T07:00:00+02:00`. Add an Age or Distance category.
5. Use a separate private browser session for a Participant, register/login, then enrol with the created category ID. Each browser session keeps its own account.
6. Show a **403** when a Participant tries an organiser action; show a **409** for a duplicate enrolment. Result capture requires the event start to have passed; use a separate past-date demo event and create its enrolment before moving the date back.
7. Log out with `POST /api/auth/logout`. Protected requests then return **401**.

The application hashes passwords with ASP.NET Core's salted password hasher (210,000 iterations), keeps user ID and role in its server ticket store, and issues a 30-minute Secure, HttpOnly, SameSite=Strict session cookie. Logout revokes the server ticket. Restarting this single-instance API signs everyone out. Public event reads do not require login; organiser writes and private lists require event ownership; participant updates and histories use the authenticated participant ID.

## Testing

Run the portable suite from `part2`:

```powershell
dotnet test RaceDay.slnx --configuration Release --logger trx --results-directory TestResults
```

The HTTP tests use ASP.NET Core's real request pipeline and cookies with an isolated SQLite relational database per test. They check registration/login/logout and revoked-cookie replay, validation, authentication, roles, ownership, event/category management, enrolments, duplicates, time windows and result histories. Unit tests exercise category bounds, decimal precision and SQL TIME limits. Direct database tests bypass DTOs to prove unique, check, foreign-key and cascade constraints are enforced.

SQLite is **not** proof of SQL Server compatibility. Two SQL Server-only tests are explicitly skipped without `RACEDAY_TEST_SQLSERVER`. To run the entire suite against real SQL Server:

```powershell
$env:RACEDAY_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test RaceDay.slnx --configuration Release --logger trx --results-directory TestResults/SqlServer
Remove-Item Env:RACEDAY_TEST_SQLSERVER
```

Use a local test instance and an account allowed to create databases. Each test creates, migrates and removes only its own randomly named `RaceDayTests_<guid>` database. SQL Server-only tests also verify actual SQL date/time/decimal round trips and NVARCHAR length enforcement. Setting the SQL test variable to an unavailable instance produces a failure, not a false passing fallback.

## Part 2 GitHub Actions and evidence

The [Part 2 workflow](.github/workflows/part2-api.yml) restores, builds and tests on Ubuntu with SQLite and on Windows with real SQL Server LocalDB. Both jobs retain TRX results, and failures fail the workflow. The [Part 1 workflow](.github/workflows/part1-validate.yml) is preserved.

- [Part 2 workflow runs](https://github.com/st10490392/PROG6212W_POE/actions/workflows/part2-api.yml): see GitHub for the actual run status.
- CI screenshot placeholder: **capture and add the actual passing Part 2 run after verification**.
- Swagger screenshot placeholder: **capture authenticated event creation and participant enrolment**.
- Database screenshot placeholder: **capture the EF-created tables and example relationships in SSMS**.
- Demonstration-video placeholder: **add the student's actual unlisted presentation URL**.

## Part 2 AI-assistance disclosure

OpenAI Codex assisted with API implementation, tests, CI configuration and documentation. AI assistance is identified in the implementation commits. The student must review, understand and adapt this work, verify the actual test/CI evidence, and explain the design independently before submission. The generated implementation is not presented as exclusively human-authored work. No passing CI image, presentation recording or video URL is invented.

See [the presentation guide](part2/PRESENTATION_GUIDE.md) for concrete code examples to demonstrate, and [the verification report](part2/VERIFICATION_REPORT.md) for actual results and remaining limitations.
