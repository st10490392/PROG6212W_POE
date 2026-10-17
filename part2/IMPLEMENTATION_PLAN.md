# Part 2 — RaceDay API implementation checklist

This branch builds on the preserved Part 1 planning artifacts in `docs/`.

## Source of truth
- `docs/API_Endpoint_Plan.md`: exact HTTP routes, role requirements, payloads and status codes.
- `docs/RaceDay_Database.sql`: six-table SQL Server schema and constraints.
- `PROG6212W.pdf` (coursework handout): Part 2 requirements and marking rubric.

## Implementation milestones
1. Scaffold ASP.NET Core Web API, EF Core SQL Server, OpenAPI/Swagger, and test project.
2. Map Users, EventTypes, Events, Categories, Enrolments and Results with matching EF constraints.
3. Implement registration/login/logout with hashed passwords and server-side sessions; restrict cookies and enforce roles.
4. Implement own-profile endpoints and public event/event-type reads.
5. Implement organiser-owned event and category CRUD with ownership checks.
6. Implement participant enrolment CRUD with category validation and duplicate prevention.
7. Implement organiser results and participant history.
8. Add API integration tests for successful and rejected requests, including unauthenticated access, cross-role and cross-owner denial.
9. Add GitHub Actions build/test workflow; confirm passing run.
10. Update root README with setup, SQL Server configuration, Swagger usage, CI screenshot and student-recorded video link.

## Verification gates
- No changes to Part 1 documentation unless an explicit discrepancy is documented.
- No fabricated CI screenshots, test outcomes, or video URLs.
- No credentials in source control.
- Make 20 **meaningful** commits reflecting actual independently reviewable work, not empty/artificial commits.
- Do not claim completion until the API builds and tests pass.
- Student must understand, review and disclose AI-assisted work according to the coursework instructions.
