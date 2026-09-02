# RaceDay

RaceDay is a planned South African event-management system for road-running, walking and cycling events. Part 1 documents the database design, API structure and SQL Server schema that will guide the implementation in later parts of the POE.

## Student details

- Name: Bono Nenguda ST10484954
- Module: Programming 2B
- Module code: PROG6212
- Assessment: POE Part 1

## User roles

### Organiser

An Organiser can create and manage events, define event categories, view enrolments, update enrolment statuses and capture participant results.

### Participant

A Participant can register and log in, manage a profile, browse events, select a category, enrol in an event and view race-result history.

## Part 1 deliverables

| Deliverable | File |
|---|---|
| Entity Relationship Diagram | [`docs/RaceDay_Part1_ERD.pdf`](docs/RaceDay_Part1_ERD.pdf) |
| API Endpoint Plan | [`docs/RaceDay_API_Endpoint_Plan.pdf`](docs/RaceDay_API_Endpoint_Plan.pdf) |
| SQL Database Script | [`docs/RaceDay_Database.sql`](docs/RaceDay_Database.sql) |

## Database design

The RaceDay database contains six related entities:

1. `Role` stores the Organiser and Participant roles.
2. `User` stores registered RaceDay users.
3. `Event` stores running, walking and cycling events.
4. `Category` stores the age or distance categories for each event.
5. `Enrolment` connects a Participant to an Event and selected Category.
6. `Result` stores the finish time and finishing position for an enrolment.

`Enrolment` resolves the many-to-many relationship between Participants and Events. A unique constraint prevents a Participant from enrolling in the same Event more than once. A Result is linked to one Enrolment, and its foreign key is unique so that an enrolment cannot receive more than one final result.
