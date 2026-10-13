# RaceDay – Programming 2B Part 1

RaceDay is a full-stack event-management platform for South African road running, walking, and cycling events. This repository contains the planning artifacts and database schema for Part 1 of the Portfolio of Evidence (PoE).

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

## AI disclosure

AI tools may be used for planning, proofreading, or coding support only if the use is disclosed in line with the assessment rules. Any AI-generated content must be reviewed, understood, and adapted before submission, and must not be submitted as the student’s own work without their own analysis and critical thinking.

---

## Project status update — 13 October 2026

RaceDay is academic event-management coursework. The documented ERD, SQL Server schema and API plan are planning deliverables; no production service or October 2026 launch is claimed.
