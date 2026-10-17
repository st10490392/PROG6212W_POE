# Endpoint implementation and test map

This maps every operation in the unchanged Part 1 endpoint plan to its controller and meaningful HTTP tests. The actual run outcome is recorded separately in `VERIFICATION_REPORT.md`.

| Method and route | Controller | Main test coverage |
|---|---|---|
| POST `/api/auth/register` | Auth | AuthTests: both roles, password hashing, duplicate email, invalid fields |
| POST `/api/auth/login` | Auth | AuthTests: valid/invalid credentials, cookie settings, session rotation |
| POST `/api/auth/logout` | Auth | AuthTests: logout, old-cookie replay, unauthenticated logout, origin rejection |
| GET `/api/users/me` | Users | AuthTests, ProfileTests: authentication and safe own profile |
| PUT `/api/users/me` | Users | ProfileTests: persistence, validation, role/identity overposting |
| GET `/api/events` | Events | EventTests: upcoming-only, type/date filters, maximum date boundary |
| GET `/api/events/{eventId}` | Events | EventTests, CategoryTests: public detail, categories and 404 |
| POST `/api/events` | Events | EventTests: owner assignment, event-type validation |
| PUT `/api/events/{eventId}` | Events | EventTests: update and cross-owner denial |
| DELETE `/api/events/{eventId}` | Events | EventTests: deletion, ownership and enrolled-event conflict |
| GET `/api/event-types` | EventTypes | EventTests: public Run/Walk/Cycle seeds |
| GET `/api/events/{eventId}/categories` | Categories | CategoryTests: public list and missing event |
| POST `/api/events/{eventId}/categories` | Categories | CategoryTests: creation, age/distance validation and ownership |
| PUT `/api/categories/{categoryId}` | Categories | CategoryTests: correction and cross-owner denial |
| DELETE `/api/categories/{categoryId}` | Categories | CategoryTests, EventTests: deletion, ownership, used-category conflict |
| POST `/api/events/{eventId}/enrolments` | Enrolments | EnrolmentTests: valid entry, duplicate, wrong/missing category, closed window |
| GET `/api/users/me/enrolments` | Enrolments | EnrolmentTests: isolation between participants |
| GET `/api/events/{eventId}/enrolments` | Enrolments | EnrolmentTests, EventTests: owner's list and cross-owner denial |
| PUT `/api/enrolments/{enrolmentId}` | Enrolments | EnrolmentTests: category change, ownership, category membership, start boundary |
| DELETE `/api/enrolments/{enrolmentId}` | Enrolments | EnrolmentTests: cancellation, ownership, 404 and closed window |
| POST `/api/enrolments/{enrolmentId}/result` | Results | ResultTests: capture, owner, duplicate, pre-start conflict, invalid finish time |
| PUT `/api/results/{resultId}` | Results | ResultTests: correction, cross-owner denial and missing result |
| GET `/api/users/me/results` | Results | ResultTests: personal history and another participant's empty history |
| GET `/api/events/{eventId}/results` | Results | ResultTests, EventTests: owner's results and cross-owner denial |

`PermissionTests` additionally checks **all 18 protected operations** without authentication and all organiser-only/participant-only operations with the opposite role. `SwaggerTests` checks all 24 operations are exposed. `ValidationTests` and `SessionStoreTests` provide unit-level boundary/session checks. `DatabaseTests` verifies real relational constraints independently of HTTP validation. `ErrorTests` checks safe unexpected-error output.

When `RACEDAY_TEST_SQLSERVER` is configured, the same HTTP and database suite uses SQL Server and applies the checked-in migration. `SqlServerTests` checks provider-specific column behaviour. Without the variable, these SQL-only tests are explicitly skipped and portable tests use SQLite.
