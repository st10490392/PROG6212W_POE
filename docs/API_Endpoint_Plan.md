# RaceDay – Part 1 API Endpoint Plan

**Module:** Programming 2B (PROG6212)  
**Part:** 1 – System Planning and Database  
**System:** RaceDay  

> This document is a planning specification for Part 2. The implemented API should closely follow these routes and contracts. Role names are `Organiser` and `Participant`; `Any` means any authenticated user; `None` means public.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body (if any) | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new RaceDay account for an Organiser or Participant. | None | `{ firstName, lastName, email, password, role, phone?, dateOfBirth? }` | **201 Created** user summary; **400** validation error; **409** email already registered. |
| POST | `/api/auth/login` | Validates credentials and creates the authenticated server-side session containing user ID and role. | None | `{ email, password }` | **200 OK** user/session summary; **401** invalid credentials. |
| POST | `/api/auth/logout` | Ends the current authenticated session. | Any | None | **204 No Content**; **401** not authenticated. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body (if any) | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | Returns the authenticated user's own profile. | Any | None | **200 OK** profile; **401** not authenticated. |
| PUT | `/api/users/me` | Updates the authenticated user's profile information. | Any | `{ firstName, lastName, phone?, dateOfBirth?, profilePictureUrl? }` | **200 OK** updated profile; **400** invalid data; **401** not authenticated. |

## Events

| HTTP Method | Route | Description | Role Required | Request Body (if any) | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Returns upcoming events, with optional filtering for event type and date. | None | None | **200 OK** array of events; **500** server error. |
| GET | `/api/events/{eventId}` | Returns full details for one event, including its categories. | None | None | **200 OK** event detail; **404** event not found. |
| POST | `/api/events` | Creates a new event owned by the authenticated Organiser. | Organiser | `{ name, description, eventDate, location, distanceKm, eventTypeId }` | **201 Created** event; **400** validation error; **401/403** unauthorised. |
| PUT | `/api/events/{eventId}` | Updates an event created by the authenticated Organiser. | Organiser | `{ name, description, eventDate, location, distanceKm, eventTypeId }` | **200 OK** updated event; **403** not owner; **404** not found; **400** validation error. |
| DELETE | `/api/events/{eventId}` | Deletes an event created by the authenticated Organiser. | Organiser | None | **204 No Content**; **403** not owner; **404** not found; **409** event has enrolments. |

## Event Types

| HTTP Method | Route | Description | Role Required | Request Body (if any) | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/event-types` | Returns the available event types: Run, Walk and Cycle. | None | None | **200 OK** array of event types. |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body (if any) | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Returns all categories available for an event. | None | None | **200 OK** category list; **404** event not found. |
| POST | `/api/events/{eventId}/categories` | Adds an age- or distance-based category to an Organiser's event. | Organiser | `{ categoryType, name, minAge?, maxAge?, distanceKm? }` | **201 Created** category; **400** invalid category; **403** not owner; **404** event not found. |
| PUT | `/api/categories/{categoryId}` | Updates an existing category belonging to an Organiser's event. | Organiser | `{ categoryType, name, minAge?, maxAge?, distanceKm? }` | **200 OK** updated category; **403** not owner; **404** not found. |
| DELETE | `/api/categories/{categoryId}` | Removes a category that has not been used by an enrolment. | Organiser | None | **204 No Content**; **403** not owner; **404** not found; **409** category already used. |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body (if any) | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/enrolments` | Enrols the logged-in Participant in an event and records the selected category. | Participant | `{ categoryId }` | **201 Created** enrolment; **400** invalid category; **403** wrong role; **404** event/category not found; **409** already enrolled. |
| GET | `/api/users/me/enrolments` | Returns all events the logged-in Participant has entered. | Participant | None | **200 OK** enrolment list; **403** wrong role. |
| GET | `/api/events/{eventId}/enrolments` | Returns all participant enrolments for an event owned by the Organiser. | Organiser | None | **200 OK** enrolment list; **403** not owner; **404** event not found. |
| PUT | `/api/enrolments/{enrolmentId}` | Allows the Participant to change their selected category before the event. | Participant | `{ categoryId }` | **200 OK** updated enrolment; **403** not owner/wrong role; **404** not found; **409** event already started. |
| DELETE | `/api/enrolments/{enrolmentId}` | Cancels the Participant's own enrolment before the event. | Participant | None | **204 No Content**; **403** wrong participant; **404** not found; **409** cancellation window closed. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body (if any) | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/enrolments/{enrolmentId}/result` | Captures a participant's finish time and finishing position after the event. | Organiser | `{ finishTime, finishingPosition }` | **201 Created** result; **403** not event organiser; **404** enrolment not found; **409** result already recorded. |
| PUT | `/api/results/{resultId}` | Corrects an existing result recorded by the event Organiser. | Organiser | `{ finishTime, finishingPosition }` | **200 OK** updated result; **403** not event organiser; **404** not found. |
| GET | `/api/users/me/results` | Returns the authenticated Participant's personal race history. | Participant | None | **200 OK** results with event, date, category, finish time and position; **403** wrong role. |
| GET | `/api/events/{eventId}/results` | Returns published results for an event managed by the Organiser. | Organiser | None | **200 OK** result list; **403** not owner; **404** event not found. |

## Planned API Conventions

- All routes use the `/api/` prefix.
- JSON is used for request and response payloads.
- `201 Created` is used for successful resource creation, `200 OK` for successful reads/updates, and `204 No Content` for successful deletes/logouts.
- `400 Bad Request` is used for validation failures, `401 Unauthorized` for unauthenticated requests, `403 Forbidden` for authenticated users in the wrong role or without ownership, `404 Not Found` when a resource does not exist, and `409 Conflict` when a business rule prevents the operation.
- Server-side session state should preserve the authenticated `UserId` and `Role` for protected requests.
- The API should enforce the Organiser/Participant distinction; the MVC front end in Part 3 should reflect the same role separation.
