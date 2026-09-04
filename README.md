# RaceDay – Programming 2B Part 1

RaceDay is a planned full-stack event-management platform for South African road running, walking and cycling events. Part 1 documents the database model and API specification before implementation begins.

## Roles

- **Organiser:** creates, edits and deletes events, manages categories, views enrolments for owned events and captures results.
- **Participant:** creates an account, browses events, enters events by selecting a category, views personal enrolments and tracks personal results.

## Part 1 contents

The `/docs` folder contains:

- `RaceDay_ERD.png` – Entity Relationship Diagram with keys, relationships and cardinality.
- `API_Endpoint_Plan.md` – full endpoint planning table covering authentication, profile, events, categories, enrolments and results.
- `RaceDay_Database.sql` – SQL Server schema, constraints and seed data.

## Database design

The ERD contains six entities: Users, EventTypes, Events, Categories, Enrolments and Results. The SQL script is designed to match this model exactly.

## Running the SQL script

1. Open SQL Server Management Studio (SSMS).
2. Open `docs/RaceDay_Database.sql`.
3. Execute the complete script on a SQL Server instance.
4. Confirm that `RaceDayDB` is created and the verification queries return the seeded records.

The supplied seed password strings are demonstration values for Part 1 data only. Part 2 should generate and store the actual ASP.NET-compatible password hashes.

## GitHub / CI

The Part 1 GitHub Actions workflow is stored in `.github/workflows/part1-validate.yml`. It validates the required repository structure and required planning files.

**CI screenshot:** `T

## Video presentation

**YouTube:** 

The video should demonstrate the ERD, explain the endpoint planning decisions, and run the SQL script live in SSMS.

## Student details

- **Student name:*RIPFUMELO VUNENE NGOBENI* 
- **Student number:*ST10490392*
- **Module:** PROG6212 Programming 2B
- **Assessment:** Part 1 – System Planning and Database

## AI disclosure

The assessment brief states that AI use for planning, proofreading or coding must be disclosed briefly, and that AI-generated code must not be submitted as the student's own work without the student's analysis, synthesis and critical thinking. Review, understand, test and adapt this material before using any of it in the submission, and include the required disclosure according to your institution's rules.
