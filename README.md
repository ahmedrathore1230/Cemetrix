# CEMETRIX — Enterprise Graveyard Management System

A complete, production-ready full-stack enterprise application for managing cemeteries, graves, burial records, bookings, visitors, and reports.

Built end-to-end with **.NET 8**, **Blazor Server**, **ASP.NET Core Web API**, **Entity Framework Core**, **SQL Server LocalDB**, **ASP.NET Core Identity + JWT**, **AutoMapper**, **FluentValidation**, **Serilog**, **MailKit**, **Chart.js**, and **Bootstrap 5**.

---

## Table of Contents

1. [Architecture](#architecture)
2. [Tech Stack](#tech-stack)
3. [Project Structure](#project-structure)
4. [Features](#features)
5. [Prerequisites](#prerequisites)
6. [Quick Start](#quick-start)
7. [Database Setup](#database-setup)
8. [Running the Application](#running-the-application)
9. [Default Seed Accounts](#default-seed-accounts)
10. [API Documentation](#api-documentation)
11. [Configuration](#configuration)
12. [Troubleshooting](#troubleshooting)

---

## Architecture

CEMETRIX follows **Clean Architecture** with strict separation of concerns:

```
┌──────────────────────────────────────────────────────────────────┐
│                         CEMETRIX.Web                              │
│     Blazor Server UI · Pages · Components · Layout · Theme        │
│         (cookie auth via SignInManager + AccountController)       │
└──────────────────────────────────────────────────────────────────┘
                              │
                              │   shares Application + Infrastructure DI
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                         CEMETRIX.API                              │
│      REST Controllers · JWT Auth · Swagger · Middleware           │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                    CEMETRIX.Application                           │
│   Services · DTOs · Interfaces · Validators · AutoMapper Profiles │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                   CEMETRIX.Infrastructure                         │
│  EF Core DbContext · Repositories · UoW · Identity · JWT · Email  │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                       CEMETRIX.Domain                             │
│             Entities · Enums · Base Classes · Constants           │
└──────────────────────────────────────────────────────────────────┘
```

### Patterns Used

- **Clean / Layered Architecture** — Domain → Application → Infrastructure → API/Web
- **Repository + Unit of Work** — abstract persistence behind interfaces
- **DTO Pattern** — clean contracts between layers
- **Service Layer** — encapsulates business rules
- **Dependency Injection** — everything resolved via DI
- **SOLID Principles** — single responsibility, interface segregation, etc.
- **Async/Await everywhere** — non-blocking I/O end-to-end
- **Standardized `ApiResponse<T>`** — consistent envelope for all API responses
- **Global Exception Middleware** — centralized error handling for the API

---

## Tech Stack

| Layer            | Technologies                                                                 |
|------------------|------------------------------------------------------------------------------|
| Frontend         | Blazor Server (.NET 8), Razor Components, Bootstrap 5, Bootstrap Icons, Chart.js, Blazored.Toast, Blazored.LocalStorage |
| Backend API      | ASP.NET Core 8 Web API, Swashbuckle (Swagger)                                |
| ORM / Database   | EF Core 8, SQL Server LocalDB, Code-First Migrations                         |
| Auth             | ASP.NET Core Identity, JWT Bearer Tokens, Refresh Tokens, Cookie Auth (Web)  |
| Mapping / Valid. | AutoMapper 13, FluentValidation 11                                           |
| Logging          | Serilog (Console + Rolling File)                                             |
| Email            | MailKit (SMTP)                                                               |
| Security         | BCrypt (via Identity), HTTPS, antiforgery, role-based authorization          |

---

## Project Structure

```
CEMETRIX/
├── CEMETRIX.sln
├── global.json                      # pins .NET 8 SDK
├── README.md
└── src/
    ├── CEMETRIX.Domain/             # Entities, Enums, BaseEntity, UserRoles
    ├── CEMETRIX.Application/        # DTOs, Interfaces, Services, Validators, AutoMapper
    ├── CEMETRIX.Infrastructure/     # DbContext, Repositories, UoW, Identity, JWT, Email, Migrations, Seeder
    ├── CEMETRIX.API/                # REST API, Controllers, Middleware, Swagger
    └── CEMETRIX.Web/                # Blazor Server frontend (pages, layout, components, wwwroot)
```

---

## Features

### Core
- **Dashboard** — total/available/occupied/reserved/expiring grave KPIs, revenue charts, recent burials, activity feed, animated counters, Chart.js charts.
- **Interactive Graveyard Map** — Excel-like grid by section/row/column, color-coded statuses, hover glow & tooltips, click → grave detail.
- **Grave Detail** — deceased profile, timeline, documents, QR placeholder, business rule for grave reuse (7-year minimum age), Edit/Delete/Print/Export/Book-Again/Mark-Available actions.
- **Book Grave Workflow** — multi-step form: person, burial details, grave selector, dates, payment status, validation.
- **Burial Records** — search, filters (name/DOB/DOD/section/year/status), sorting, pagination, export-ready.
- **Notifications**, **Reports**, **Visitor Logs**, **Staff Management**, **Settings**, **Help**.

### Identity & Security
- ASP.NET Core Identity with `ApplicationUser` extending `IdentityUser`.
- Four roles: **Admin**, **Manager**, **Staff**, **Viewer**.
- JWT access tokens + refresh tokens (API), cookie auth (Blazor Server).
- Login, Register, Forgot/Reset Password, Change Password, role guards on routes.

### Infrastructure
- EF Core 8 + SQL Server LocalDB, Code-First migrations, full relationships, soft-deletes via `IsDeleted`.
- Repositories + `IUnitOfWork`.
- Serilog logging to console and `logs/cemetrix-.log` (rolling daily).
- Global exception middleware → standardized `ApiResponse<T>` errors.
- MailKit-based SMTP `EmailService` with Welcome / Forgot-Password / Notification / Expiration templates.
- Audit log + activity log entities baked into the schema.
- CORS configured for Blazor ↔ API.

### UI / UX
- Premium glassmorphism design — elegant greens, dark slate, gold/red alerts.
- Dark mode (persists via localStorage).
- Reusable components: `StatCard`, `ChartCard`, `Modal`, `Pagination`, `EmptyState`, `LoadingScreen`, `Breadcrumbs`.
- Toast notifications (Blazored.Toast).
- Fully responsive: desktop, tablet, mobile (collapsible sidebar).
- Skeleton loaders, smooth transitions, hover micro-animations.
- 404, Access Denied, Error pages.

---

## Prerequisites

- **.NET 8 SDK** — verify with `dotnet --version` (must be `8.0.x`).
- **SQL Server LocalDB** — bundled with Visual Studio or installable standalone.
  - Verify: `sqllocaldb info`
- Optional: **Visual Studio 2022**, **VS Code** (with C# Dev Kit), or **JetBrains Rider**.

---

## Quick Start

```powershell
# 1. From the repo root
cd CEMETRIX

# 2. Restore + build everything
dotnet restore
dotnet build

# 3. Run the API (creates DB + seeds data on first start)
dotnet run --project src/CEMETRIX.API --launch-profile http
# API + Swagger: http://localhost:5051/swagger

# 4. In a SECOND terminal, run the Blazor Server frontend
dotnet run --project src/CEMETRIX.Web --launch-profile http
# Web UI: http://localhost:5099
```

That's it — no manual migration step is required. The first run of either project auto-applies migrations and seeds data.

---

## Database Setup

### Auto-Migration (default)

Both `CEMETRIX.API` and `CEMETRIX.Web` call `DbSeeder.SeedAsync()` on startup, which:

1. Runs any pending EF Core migrations (`db.Database.MigrateAsync()`).
2. Creates roles (`Admin`, `Manager`, `Staff`, `Viewer`).
3. Creates seed users.
4. Seeds 60+ graves across 6 sections, deceased persons, bookings, visitors, notifications, and activity logs.

### Manual Migration (advanced)

If you want to manage migrations explicitly:

```powershell
# Install the EF Core CLI (one-time)
dotnet tool install --global dotnet-ef --version 8.0.10

# Add a new migration
dotnet ef migrations add MyMigrationName `
  --project src/CEMETRIX.Infrastructure `
  --startup-project src/CEMETRIX.API `
  --output-dir Persistence/Migrations

# Apply migrations to the database
dotnet ef database update `
  --project src/CEMETRIX.Infrastructure `
  --startup-project src/CEMETRIX.API
```

### Reset Database

```powershell
dotnet ef database drop -f `
  --project src/CEMETRIX.Infrastructure `
  --startup-project src/CEMETRIX.API
```

Then re-run the API to recreate and reseed.

---

## Running the Application

### Option A — Two-terminal local dev (recommended)

| Process                | Command                                                                                  | URL                              |
|------------------------|------------------------------------------------------------------------------------------|----------------------------------|
| API (REST + Swagger)   | `dotnet run --project src/CEMETRIX.API --launch-profile http`                            | http://localhost:5051/swagger    |
| Web (Blazor Server)    | `dotnet run --project src/CEMETRIX.Web --launch-profile http`                            | http://localhost:5099            |

For HTTPS variants, swap `--launch-profile http` → `--launch-profile https`.

### Option B — Visual Studio 2022

1. Open `CEMETRIX.sln`.
2. Right-click the solution → **Set Startup Projects… → Multiple startup projects**:
   - `CEMETRIX.API` → **Start**
   - `CEMETRIX.Web` → **Start**
3. Press **F5**.

### Option C — JetBrains Rider

1. Open `CEMETRIX.sln`.
2. Configure a compound run config combining the API and Web profiles.
3. Run.

### Option D — VS Code

1. Open the workspace folder.
2. Use the integrated terminal and run two `dotnet run` commands as in Option A.

---

## Default Seed Accounts

All seeded accounts share the same password: **`Admin@12345`**

| Role     | Email                  | Name             | Job Title             |
|----------|------------------------|------------------|-----------------------|
| Admin    | `admin@cemetrix.com`   | Sarah Williams   | System Administrator  |
| Manager  | `manager@cemetrix.com` | Daniel Chen      | Cemetery Manager      |
| Staff    | `staff@cemetrix.com`   | Maria Lopez      | Records Officer       |

> **Change these credentials immediately in production.**

---

## API Documentation

When the API is running, full interactive Swagger UI is available at:

- **http://localhost:5051/swagger**

### Auth Endpoints

| Method | Endpoint                       | Description                       |
|--------|--------------------------------|-----------------------------------|
| POST   | `/api/auth/register`           | Register a new user               |
| POST   | `/api/auth/login`              | Get JWT + refresh token           |
| POST   | `/api/auth/refresh`            | Refresh JWT                       |
| POST   | `/api/auth/forgot-password`    | Send reset email                  |
| POST   | `/api/auth/reset-password`     | Reset password with token         |
| POST   | `/api/auth/change-password`    | Change password (auth required)   |
| POST   | `/api/auth/logout`             | Revoke refresh token              |
| GET    | `/api/auth/me`                 | Current user profile              |

### Resource Endpoints

`/api/graves`, `/api/deceased`, `/api/bookings`, `/api/notifications`, `/api/visitors`, `/api/dashboard`, `/api/users` — full CRUD with pagination, search, filter, sort.

### Standardized Response Envelope

```json
{
  "success": true,
  "message": "OK",
  "data": { /* ... */ },
  "errors": null,
  "statusCode": 200
}
```

### Authenticating Swagger Requests

1. Call `POST /api/auth/login` with seed credentials.
2. Copy the returned `accessToken`.
3. Click **Authorize** in Swagger and paste `Bearer <token>`.

Example login (PowerShell):

```powershell
$body = '{"email":"admin@cemetrix.com","password":"Admin@12345"}'
Invoke-RestMethod -Uri "http://localhost:5051/api/auth/login" `
  -Method POST -Body $body -ContentType "application/json"
```

---

## Configuration

All configuration lives in `appsettings.json` of each runnable project (`CEMETRIX.API`, `CEMETRIX.Web`).

### Connection String

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CEMETRIX_Db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Point this to any SQL Server instance you want.

### JWT

```json
"Jwt": {
  "Issuer": "CEMETRIX",
  "Audience": "CEMETRIX.Clients",
  "Secret": "CHANGE_ME_IN_PRODUCTION",
  "AccessTokenExpirationMinutes": 60,
  "RefreshTokenExpirationDays": 7
}
```

> Rotate `Secret` before going to production. Use at least 256-bit entropy.

### Email (SMTP)

```json
"Email": {
  "SmtpServer": "smtp.gmail.com",
  "Port": 587,
  "Username": "",
  "Password": "",
  "FromName": "CEMETRIX",
  "FromEmail": "no-reply@cemetrix.local",
  "UseSsl": true,
  "EnableSending": false,
  "BaseUrl": "https://localhost:5001"
}
```

Set `EnableSending: true` and fill in your SMTP credentials to enable outbound mail (welcome, password reset, expiration alerts, notifications).

---

## Troubleshooting

### `sqllocaldb` not found / cannot connect to LocalDB

- Install SQL Server Express LocalDB: https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb
- Or change the connection string to point to a real SQL Server instance.

### `dotnet ef` not recognized

```powershell
dotnet tool install --global dotnet-ef --version 8.0.10
```

Then re-open your terminal.

### Build warnings about NU1903 / AutoMapper / MailKit

Known transitive advisories — the app builds and runs cleanly. To suppress in your environment, upgrade the packages in each `.csproj` and rebuild.

### Port already in use

Edit the `applicationUrl` in `src/CEMETRIX.API/Properties/launchSettings.json` or `src/CEMETRIX.Web/Properties/launchSettings.json`.

### Reset everything

```powershell
dotnet ef database drop -f --project src/CEMETRIX.Infrastructure --startup-project src/CEMETRIX.API
dotnet build
dotnet run --project src/CEMETRIX.API
```

---

## License

Proprietary — © CEMETRIX. All rights reserved.
