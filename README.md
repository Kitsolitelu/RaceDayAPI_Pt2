# RaceDay API - Part 2 (RESTful API Development)

**Student:** \[Kitso Litelu - ST10486804]

RaceDay is a RESTful API for managing South African road running, walking and cycling events. Organisers create events and categories and capture results. Participants browse events, enrol in a category and view their own enrolments and results. This is Part 2 of the RaceDay Portfolio of Evidence (POE).

**Demo video:** 

## Technologies

* C# and ASP.NET Core Web API (.NET 10)
* Entity Framework Core (code-first) with SQL Server (LocalDB)
* Cookie authentication for session management, with password hashing (`PasswordHasher`)
* Swagger UI to view and test every endpoint
* xUnit and `WebApplicationFactory` for unit tests (in-memory database)
* GitHub Actions for CI

## The two roles

**Organiser**

* Creates, updates and deletes their own events (name, description, date, location, event type and distance)
* Adds categories to their events (for example 10km or Under 20)
* Views all enrolments for their events
* Captures finish times and positions for enrolments in their events

**Participant**

* Views events and categories (anyone can, no login needed)
* Enrols in a category
* Views their own enrolments and their own results

Both roles can register, log in, log out, and view and update their own profile. Organiser endpoints return **403 Forbidden** for a Participant and Participant endpoints return **403 Forbidden** for an Organiser. Endpoints that need a login return **401 Unauthorized** when nobody is logged in.

To stop anyone from making themselves an Organiser, registering as an Organiser needs an **organiser access code**. It is set in `appsettings.json` under `OrganiserSettings:AccessCode` and is `RACEDAY-ORG-2026`.

## Setup and run instructions

**You need:** Visual Studio with the ASP.NET workload, the .NET 10 SDK, and SQL Server LocalDB (installed with Visual Studio).

1. Clone the repo and switch to the Part 2 branch:

```
   git clone <repo link>
   git checkout part-2-restful-api
   ```

2. Open the `.slnx` solution file in Visual Studio.
3. Check the connection string in `RaceDayAPI\_Pt2/appsettings.json`. It uses LocalDB by default:

```
   Server=(localdb)\\\\MSSQLLocalDB;Database=RaceDayDB;Trusted\_Connection=True;TrustServerCertificate=True;
   ```

   If you use SQL Server Express, change the server to `.\\\\SQLEXPRESS`.

4. Create the database. Open **Tools > NuGet Package Manager > Package Manager Console**, set **Default project** to `RaceDayAPI\_Pt2` and run:

```
   Update-Database
   ```

5. Run the API with the green **https** button.
6. Look in the console window for the line `Now listening on: https://localhost:XXXX` and open `https://localhost:XXXX/swagger` in your browser.

### Trying it out in Swagger

1. `POST /api/auth/register` to create a Participant, and again for an Organiser (with `organiserCode` set to `RACEDAY-ORG-2026`).
2. `POST /api/auth/login`. Swagger keeps the session cookie, so you stay logged in.
3. Log in as the Organiser and create an event, then add a category to it.
4. Log out (`POST /api/auth/logout`), log in as the Participant and enrol in the category.
5. Log in as the Organiser again and capture a result for the enrolment.

### Running the unit tests

In Visual Studio open **Test > Test Explorer** and click **Run All Tests**, or run `dotnet test` in the solution folder.

## Endpoints

|Method|Route|Description|Who can use it|
|-|-|-|-|
|POST|/api/auth/register|Create an account as Organiser or Participant|Anyone|
|POST|/api/auth/login|Log in and start a session|Anyone|
|POST|/api/auth/logout|Log out and end the session|Logged in|
|GET|/api/users/me|View my profile|Logged in|
|PUT|/api/users/me|Update my profile|Logged in|
|GET|/api/users/me/enrolments|View my enrolments|Participant|
|GET|/api/users/me/results|View my results|Participant|
|GET|/api/events|List upcoming events|Anyone|
|GET|/api/events/{id}|View one event|Anyone|
|POST|/api/events|Create an event|Organiser|
|PUT|/api/events/{id}|Update my event|Organiser (owner)|
|DELETE|/api/events/{id}|Delete my event|Organiser (owner)|
|GET|/api/events/{id}/categories|List an event's categories|Anyone|
|POST|/api/events/{id}/categories|Add a category to my event|Organiser (owner)|
|GET|/api/events/{id}/enrolments|View all enrolments for my event|Organiser (owner)|
|GET|/api/events/{id}/results|View the results for an event|Anyone|
|POST|/api/categories/{id}/enrol|Enrol in a category|Participant|
|POST|/api/enrolments/{id}/results|Capture a result for an enrolment|Organiser (owner)|

## Changes from my Part 1 plan

* **Cookie sessions instead of a token.** The Part 2 brief asks for session management, so login sets a session cookie that keeps the user logged in and remembers their role.
* **Event type and distance.** The brief requires them, so I added an `EventType` column (Run, Walk or Cycle) to `Events`. Distance is stored in `RouteInfo` and is sent when an event is created.
* **Organiser access code** when registering as an Organiser.
* **Extra endpoints** the brief needs: `GET /api/events/{id}/enrolments`, `GET /api/users/me/results` and `POST /api/auth/logout`.
* **Ownership checks.** An Organiser can only change their own events, so another Organiser gets 403.
* **Delete rules.** Deleting an event also removes its route info, categories, enrolments and results.

## Project structure

```
RaceDayAPI\_Pt2/
  Controllers/   AuthController, UsersController, EventsController,
                 CategoriesController, EnrolmentsController
  Data/          RaceDayContext (EF Core DbContext)
  DTOs/          request and response classes
  Models/        User, Event, RouteInfo, Category, Enrolment, Result
  Migrations/    EF Core migrations
  Program.cs     services, cookie authentication and Swagger
  appsettings.json
RaceDayAPI\_Pt2.Tests/
  TestHelpers.cs, AuthTests.cs, EventTests.cs,
  RoleAccessTests.cs, EnrolmentTests.cs
.github/workflows/dotnet.yml
```

## Unit tests

The tests start the real API in memory with an in-memory database, so they check the whole pipeline, including login cookies and role checks. They cover:

* Registration and login (success and failure cases)
* Event management by Organisers
* Roles being rejected when they try functionality that belongs to the other role
* Enrolments being recorded correctly, and capturing and viewing results

## CI/CD

A GitHub Actions workflow (`.github/workflows/dotnet.yml`) restores, builds and runs all unit tests on every push.

!\[Green CI build](docs/ci-green-build.png)

