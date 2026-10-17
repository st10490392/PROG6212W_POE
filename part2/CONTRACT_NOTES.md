# Part 1 contract decisions

The six entities, 24 HTTP method/route combinations and role names come from the unchanged `docs/API_Endpoint_Plan.md` and `docs/RaceDay_Database.sql`.

The supplied repository does not contain the referenced `PROG6212W.pdf` handout. Implementation and verification follow the supplied endpoint plan, SQL script, implementation plan and master-prompt requirements; a claim of checking the full unseen marking rubric would be inaccurate.

## Details the endpoint plan leaves open

- `GET /api/events?eventTypeId=1&date=2030-06-02` filters upcoming events by type ID and a UTC calendar day. Events at or before the current time are excluded. GET by ID can still retrieve past events.
- Send `eventDate` as ISO 8601 with an explicit offset, for example `2030-06-02T07:00:00+02:00`. The API converts it to UTC for `datetime2(0)` storage and labels response timestamps with `Z`. For an offset-free JSON timestamp, .NET treats it using the server's local timezone; always supply the offset for portable behaviour.
- SQL Server stores event dates at whole-second precision and rounds fractional seconds. Create/update responses reload the saved values so their dates match a subsequent GET. Regression tests cover rounding both down and up; the portable SQLite provider retains its own date precision.
- `finishTime` is a JSON string such as `"00:52:18"`. SQL `TIME(0)` permits zero through 23:59:59; fractional seconds, negative durations and durations of 24 hours or more are rejected.
- A start date is available, but no end date exists. Result writes return **409** before the start. The organiser remains responsible for confirming the race has finished. There is no separate publish flag: captured results appear in the planned owner-only event result list and the participant's own history.
- Creating an enrolment at/after the event start also returns **409**, consistent with the documented before-event update/cancellation window.
- Cancellation deletes the participant's own enrolment, allowing a subsequent enrolment before the event starts. Enrolments with results cannot be changed or deleted even if the organiser moves the event date into the future.
- A missing event/category returns **404**; an existing category belonging to another event returns **400**. Age categories validate the Part 1 age bounds; they do not introduce a participant age-eligibility rule absent from the plan.
- Registration accepts exactly `Organiser` or `Participant`, returns **201** without logging in, and requires a 12–128 character password. Emails are trimmed and stored lowercase. Login accepts the same email case-insensitively. Registration and login are limited to ten attempts per minute per client IP (**429** on exhaustion).
- Profile update DTOs cannot change email, role or user ID. Password hashes are never included in any response. Unknown JSON properties are ignored by the default serializer and cannot overpost entity fields.
- Creation responses include the created resource; event/category/enrolment/result responses use `Location` URLs to their planned readable resource or collection. No unplanned individual category/result/enrolment GET routes were added.

## Database parity

EF maps the Part 1 tables, columns, lengths, decimal/date/time types, unique indexes, checks, defaults and delete relationships. Only Run/Walk/Cycle are seeded; demo users with fake password hashes are deliberately not imported. The Part 2 database is named `RaceDayPart2`, separate from Part 1 `RaceDayDB`. Use EF migrations on an empty Part 2 database, not on top of the destructive Part 1 script.

The Part 1 database does not constrain a user's role through its foreign key or require the enrolment category's event ID to equal its event ID. The API enforces organiser/participant roles and category membership through services and controllers. It does not silently add a different composite key or trigger to the original schema. Direct writes outside the API must respect these business rules.

The portable SQLite model uses `CURRENT_TIMESTAMP` and numeric casts in distance checks because SQLite stores EF decimal values as text. These test-provider adjustments do not change the SQL Server migration or establish SQL Server compatibility; the separate SQL Server suite applies the actual migration and checks native types.

SQL Server cascade rules match Part 1. The API additionally refuses to delete events that have enrolments or categories already used by enrolments. Serializable write transactions keep these checks and writes together. Database unique/check/FK conflicts return a safe **409**. Unexpected errors return **500** without exception details; diagnostic details go to server logs.

## Authentication boundary

ASP.NET Core cookie authentication uses a singleton server ticket store backed by memory cache. The random server key is protected in an HttpOnly, Secure, SameSite=Strict session cookie; user ID and role stay in the server ticket. Absolute lifetime is 30 minutes. Login rotates the current browser's session; logout removes its server ticket, so a copied old cookie cannot authenticate. Separate browser sessions remain independent. Restarting the API clears all sessions. This is a single-instance academic application; a shared ticket store would be required for multiple instances.

For re-login, the controller revokes the ticket and obtains a fresh framework cookie handler before signing in. The [ASP.NET Core handler](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Security/Authentication/Cookies/src/CookieAuthenticationHandler.cs) retains its session key after sign-out within the same request; reusing that handler would renew the revoked key. `AuthTests` checks that the replacement session works while the old cookie fails. Session lifetime is calculated by the cookie handler from the configured 30-minute lifetime, and a separate test clock verifies expiration without waiting.

Use HTTPS and the Swagger UI served from the same API origin. Login in Swagger first; the browser automatically sends the session cookie on later requests. JavaScript cannot read the HttpOnly cookie. SameSite=Strict, JSON request bodies, no enabled CORS policy, and origin checks on unsafe methods protect browser writes. Requests with a foreign Origin or `Sec-Fetch-Site: cross-site` are rejected. Command-line clients may omit Origin but must keep the cookie jar.
