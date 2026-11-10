# Booking API

[![CI](https://github.com/Gonetc/booking-api/actions/workflows/ci.yml/badge.svg)](https://github.com/Gonetc/booking-api/actions/workflows/ci.yml) ![.NET 10](https://img.shields.io/badge/.NET-10-512BD4) ![License: MIT](https://img.shields.io/badge/license-MIT-green)

REST API for online appointment booking in a barbershop / beauty salon.
It is the backend of a small product that also includes a
[web app](https://github.com/Gonetc/booking-web) (client booking site + admin panel)
and a [Telegram bot](https://github.com/Gonetc/booking-bot).

```
 Client                          Salon admin
   ├── Telegram bot                  │
   └── Web booking site        Admin panel
              │                      │
              └──────►  Booking API  ◄┘
                            │
                        PostgreSQL
```

## Features

- **Catalog**: services (duration, price) and masters (specialization, weekly schedule, services they provide)
- **Availability**: free start times for a master, service and date — respects working hours,
  existing bookings, service duration and the current time in the salon's time zone
- **Bookings**: create, list, view, cancel; double booking of the same slot is rejected with `409 Conflict`
- **JWT authentication**: management endpoints require an admin token; the login endpoint is rate-limited against brute force
- **Soft delete** of services and masters to keep booking history intact
- **Consistent errors** in RFC 7807 `ProblemDetails` format (`400`, `404`, `409`, `422`)
- **Interactive API docs** (OpenAPI + Scalar UI)
- **Tests**: unit tests for the scheduling algorithm and HTTP-level integration tests

## Tech stack

| Area | Technology |
|---|---|
| Runtime | .NET 10, ASP.NET Core (controllers) |
| Data | Entity Framework Core 10, PostgreSQL 17 |
| Security | JWT Bearer authentication, role-based authorization, ASP.NET Core rate limiting |
| API docs | `Microsoft.AspNetCore.OpenApi`, Scalar |
| Tests | xUnit, `WebApplicationFactory`, EF Core InMemory |
| Infrastructure | Docker, Docker Compose |

## Getting started

### Option 1 — everything in Docker

```bash
docker compose up --build
```

Open <http://localhost:8080> — it redirects to the interactive API docs.
Demo data (3 services, 2 masters) is created automatically.

### Option 2 — run from the IDE

```bash
docker compose up -d db                  # start only PostgreSQL
dotnet run --project src/BookingApi      # http://localhost:5080
```

### Run tests

```bash
dotnet test
```

## API overview

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/auth/login` | Public | Get an admin JWT (5 attempts/min per IP) |
| GET | `/api/services` | Public | List services (`?includeInactive=true` for all) |
| GET | `/api/services/{id}` | Public | Get a service |
| POST | `/api/services` | Admin | Create a service |
| PUT | `/api/services/{id}` | Admin | Update a service |
| DELETE | `/api/services/{id}` | Admin | Deactivate a service |
| GET | `/api/masters` | Public | List masters (`?serviceId=` to filter) |
| GET | `/api/masters/{id}` | Public | Get a master |
| GET | `/api/masters/{id}/availability?serviceId=&date=` | Public | Free start times for a date |
| POST | `/api/masters` | Admin | Create a master with services and schedule |
| PUT | `/api/masters/{id}` | Admin | Update a master |
| DELETE | `/api/masters/{id}` | Admin | Deactivate a master |
| GET | `/api/bookings` | Admin | List bookings (`?date=&masterId=`) |
| GET | `/api/bookings/{id}` | Admin | Get a booking |
| POST | `/api/bookings` | Public | Create a booking |
| POST | `/api/bookings/{id}/cancel` | Admin | Cancel a booking |

### Authentication

```http
POST /api/auth/login
Content-Type: application/json

{ "username": "admin", "password": "admin12345" }
```

The response contains `accessToken`. Send it with admin requests:
`Authorization: Bearer <accessToken>`. In the docs UI paste it into the **Authentication** section.
The credentials above exist only in `appsettings.Development.json`.

Example — book a haircut:

```http
POST /api/bookings
Content-Type: application/json

{
  "masterId": 1,
  "serviceId": 1,
  "date": "2030-01-07",
  "startTime": "12:00:00",
  "clientName": "John Smith",
  "clientPhone": "+1 555 123 4567"
}
```

## Project structure

```
src/BookingApi
├── Controllers/   HTTP endpoints
├── Contracts/     request/response DTOs and mapping
├── Domain/        entities: SalonService, Master, WorkingHours, Booking
├── Data/          EF Core DbContext, migrations, demo data seeder
├── Services/      business logic: availability calculation, booking rules
├── Auth/          JWT and admin options, token issuing
├── Errors/        domain exceptions → ProblemDetails
├── OpenApi/       Bearer scheme for the API docs
└── Options/       typed configuration (salon time zone, slot step)
tests/BookingApi.Tests
├── SlotCalculatorTests.cs   unit tests of the scheduling algorithm
├── AuthTests.cs             login, rate limiting, protected endpoints
└── BookingFlowTests.cs      end-to-end HTTP tests with a frozen clock
```

## Configuration

| Key | Default | Description |
|---|---|---|
| `ConnectionStrings:Default` | local PostgreSQL | Database connection |
| `Salon:TimeZone` | `UTC` | IANA time zone of the salon, e.g. `Europe/Berlin` |
| `Salon:SlotStepMinutes` | `30` | Grid of possible start times |
| `Jwt:SigningKey` | — (required) | HMAC secret, at least 32 characters |
| `Jwt:ExpirationMinutes` | `60` | Access token lifetime |
| `Admin:Username` / `Admin:Password` | — (required) | Administrator credentials |
| `SeedDemoData` | `true` in Development | Fill an empty database with demo data |

## Roadmap

- [ ] GitHub Actions CI (build + tests)
- [ ] Database-level protection against concurrent double booking
- [ ] Rate limiting for public booking endpoints
- [ ] Refresh tokens and multiple staff accounts

## License

MIT
