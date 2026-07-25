# Mentivio

Mentivio is a managed online tutoring platform connecting **Students**,
**Parents**, **Tutors**, and **Admins** through role-specific dashboards.
Students and parents book sessions with tutors across subjects like Math,
Physics, and English; tutors manage their schedule, lesson notes, and
earnings; admins oversee accounts, pricing, bookings, and disputes.

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | Angular (standalone components), Bootstrap 5 + Bootstrap Icons |
| Backend | ASP.NET Core (.NET 10) Web API |
| Database | PostgreSQL, hosted on Supabase |
| Auth | JWT access tokens + DB-backed refresh-token sessions, BCrypt password hashing |

## Project structure

```
Mentivio/
├── Client/          Angular frontend
│   └── src/app/
│       ├── components/   Sidebar, Topbar, MainLayout, AuthShell, ComingSoon
│       ├── pages/         RoleSelect, Login, Register, ForgotPassword
│       ├── models/        UserRole, NavItem
│       ├── services/      RoleService, NotificationService
│       └── config/        Per-role navigation menus
└── Backend/         .NET Web API
    ├── Domain/            Entities (User, UserSession) and enums (UserRole)
    ├── Infrastructure/     EF Core DbContext, entity configs, JWT/session/password services
    ├── Models/            API request/response DTOs
    ├── Controllers/        AuthController (register, login, refresh, logout, me)
    └── Migrations/         EF Core migrations
```

## Prerequisites

- [Node.js](https://nodejs.org) (v20+) and npm
- [Angular CLI](https://angular.dev): `npm install -g @angular/cli`
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A [Supabase](https://supabase.com) project (free tier is fine) for the Postgres database
- The team's shared **JWT secret** and **Supabase database password** (get these from whoever manages the team's password manager — never from git)

---

## Setup — Backend

```bash
cd Backend
dotnet restore
```

### Configure secrets (never commit these — they're stored outside the repo via user-secrets)

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "<the team's shared JWT secret>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=aws-0-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.jgshnwgrtdcytfccuvqt;Password=<the real Supabase DB password>;SSL Mode=Require;Trust Server Certificate=true"
```

> Use Supabase's **Session pooler** connection details (Project → Connect →
> Session pooler), not "Direct connection" — the direct hostname is IPv6-only
> and won't resolve on most networks. Get the exact host from your Supabase
> dashboard; the region may differ from the example above.

### Apply database migrations

```bash
dotnet ef database update
```

This creates the `Users` and `UserSessions` tables in Supabase if they don't
already exist. Safe to re-run any time — it's a no-op if you're already up
to date.

### Run the API

```bash
dotnet run
```

The API runs on the port shown in the console output. In development,
Swagger UI is available at `/swagger` for exploring and testing endpoints
(use the "Authorize" button to paste in a token from `/api/auth/login`).

---

## Setup — Frontend

```bash
cd Client
npm install
```

Bootstrap and Bootstrap Icons are already in `package.json`. Confirm
`angular.json` includes Bootstrap's JS bundle under `scripts` (needed for
dropdowns like the notifications bell):
```json
"scripts": ["node_modules/bootstrap/dist/js/bootstrap.bundle.min.js"]
```

### Run the app

```bash
ng serve
```

Visit `http://localhost:4200` — you'll land on the role selector, then can
sign in/register per role (Student, Parent, Tutor, Admin).

---

## Running the full project

Open two terminals:

```bash
# Terminal 1
cd Backend
dotnet run

# Terminal 2
cd Client
ng serve
```

The frontend is configured to call the backend via CORS policy `AllowClient`,
which currently allows `http://localhost:4200`.

## Working with migrations (for contributors)

If you change an entity under `Backend/Domain/Entities/`:

```bash
cd Backend
dotnet ef migrations add DescriptiveNameForYourChange
dotnet ef database update
```

Commit the generated files under `Migrations/` along with your code change,
and mention the new migration in your PR description so teammates know to
run `dotnet ef database update` after pulling.

## Secrets checklist

| Secret | Where it lives | Shared how |
|---|---|---|
| `Jwt:Secret` | `dotnet user-secrets` (local) / env var (deployed) | Team password manager — must be identical for everyone, since it validates every token |
| `ConnectionStrings:DefaultConnection` | `dotnet user-secrets` (local) / env var (deployed) | Team password manager |

Never commit real values for either of these to `appsettings.json` or
`appsettings.Development.json`.