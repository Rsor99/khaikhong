# Khaikhong

Monorepo for the KhaiKhong platform. The backend ASP.NET Core solution lives in `KhaikhongService/`, and a new Next.js frontend will be added alongside it.

## Repository Layout
- `KhaikhongService/` – Production-grade Clean Architecture backend (details below)
- `frontend/` – **Next.js project placeholder** (space reserved for the upcoming app)

---

## Backend – `KhaikhongService`

Backend service built with ASP.NET Core 9 and Clean Architecture to demonstrate production-grade patterns: CQRS with MediatR, layered separation, RSA-secured authentication, and comprehensive unit testing.

### Architecture
- **Presentation (`Khaikhong.WebAPI`)** – ASP.NET Core Web API hosting, dependency injection bootstrap, middleware, and consistent `ApiResponse<T>` envelopes.
- **Application (`Khaikhong.Application`)** – CQRS commands/queries, validators, and mapping profiles coordinating work through `IUnitOfWork` abstractions.
- **Domain (`Khaikhong.Domain`)** – Framework-agnostic entities, value objects, domain events, and base entity types.
- **Infrastructure (`Khaikhong.Infrastructure`)** – EF Core persistence across identity and business contexts, repository implementations, token generation, and cross-cutting services.
- **Tests (`Khaikhong.Tests`)** – xUnit suites covering application orchestrations, domain rules, and infrastructure behaviors with in-memory providers.

### Design Principles
- Clean Architecture boundaries mandate dependency flow from outer layers inward; business logic never couples to frameworks.
- CQRS + MediatR split reads/writes, with FluentValidation guard clauses and AutoMapper translation layers.
- SOLID/DRY/KISS guide abstraction choices, keeping classes single-purpose and test-friendly.
- Security by design: RSA-signed JWTs, hashed refresh tokens, strict cookie policies, environment-driven secrets.
- Guard clauses, explicit logging metadata, and immutable state favor maintainability and predictable behavior.
- Deployment ready: environment-variable configuration, container-friendly RSA key management, and stateless API host.

### Prerequisites
- [.NET SDK 9.0](https://dotnet.microsoft.com/download)
- MySQL 8.x (or compatible) running locally or accessible via connection string
- Optional: `dotnet-ef` global tool if you plan to add migrations

### Quick Start
```bash
cd KhaikhongService

# restore dependencies
dotnet restore

# build all projects
dotnet build

# run the web API (serves Swagger at /swagger)
dotnet run --project Khaikhong.WebAPI
```

### Configuration
Configuration follows 12-factor conventions:
- `Khaikhong.WebAPI/appsettings.json` – baseline settings.
- `Khaikhong.WebAPI/appsettings.{Environment}.json` – environment overrides (e.g., `Development`).
- `.env` – sensitive secrets (connection strings, RSA keys) loaded via `AddEnvironmentVariables`.

Create a `.env` file at the solution root (`KhaikhongService/`):
```ini
CONNECTIONSTRINGS__DEFAULTCONNECTION=
JWTSETTINGS__PRIVATEKEYBASE64=
JWTSETTINGS__PUBLICKEYBASE64=
```
RSA keys are stored as base64-encoded PEM strings, decoded at startup—no filesystem key dependency.

Run `dotnet test` to execute the xUnit suites:
```bash
cd KhaikhongService
dotnet test
```

### Project Structure
```
KhaikhongService/
├── Khaikhong.Service.sln
├── Khaikhong.WebAPI          # Presentation layer (ASP.NET Core 9)
├── Khaikhong.Application     # CQRS, validators, contracts, DTOs
├── Khaikhong.Domain          # Entities, value objects, enums
├── Khaikhong.Infrastructure  # EF Core repositories, services, UoW
└── Khaikhong.Tests           # Unit/integration test projects
```

---

## Next.js Frontend (Coming Soon)

> Space intentionally left open for frontend documentation, setup steps, and deployment notes. Add details here once the Next.js project is scaffolded.

