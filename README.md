# Multitenant Identity Core

> A professional .NET 8.0 backend template showcasing multi-tenant authentication and authorization with modern patterns (DDD, CQRS, MediatR, Clean Code).

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![License](https://img.shields.io/badge/license-MIT-blue)
![DotNet](https://img.shields.io/badge/.NET-8.0-informational)

## Table of Contents

- [About](#about)
- [Key Features](#key-features)
- [Architecture & Patterns](#architecture--patterns)
- [Folder Structure](#folder-structure)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Configuration](#configuration)
  - [Run with Docker (recommended)](#run-with-docker-recommended)
  - [Run locally](#run-locally)
- [API Usage Examples](#api-usage-examples)
- [Deployment Guidance](#deployment-guidance)
- [Security Notes](#security-notes)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

## About

This repository contains a production-ready backend built with **.NET 8.0** intended to be used as a professional portfolio piece. It demonstrates a complete user management solution with:

- JWT-based authentication (Access + Refresh tokens)
- ASP.NET Core Identity integration
- Multi-tenant architecture
- Entity Framework Core with PostgreSQL
- CQRS with MediatR
- Domain-Driven Design (DDD) boundaries and Clean Code principles
- MVC-style Web API entry points

The project is intentionally modular so it can serve both as a reference implementation for interviews and as a base for production systems.

## Key Features

- ✅ **Authentication & Authorization**: JWT Access + Refresh token flow using `JwtSecurityTokenHandler` and customizable token creation.
- ✅ **Multi-Tenancy**: Tenant-aware services, database scoping and middleware to resolve tenant context per request.
- ✅ **Identity**: Full ASP.NET Identity integration with `ApplicationUser` (GUID ids supported) and extensible user profile.
- ✅ **EF Core + Postgres**: Migrations and context separation following DDD boundaries.
- ✅ **CQRS + MediatR**: Commands and Queries separated, promoting testability and single-responsibility.
- ✅ **Clean Architecture**: Layers for Core (Domain), Application, Infrastructure, and Web.
- ✅ **Extensible**: Examples for adding social login, RSA encryption hooks, and custom validation policies.

## Architecture & Patterns

High level:

```
Client -> Web (API) -> Application (CQRS + Services) -> Core (Domain) -> Infra (EF Core, Repositories)
```

- **Domain Layer (Core)**: Entities, Value Objects, Domain Events, Aggregates
- **Application Layer**: Use-cases (Commands / Queries), DTOs, Validators, Business rules
- **Infrastructure Layer**: EF Core DbContexts, Repositories, Migrations, External integrations
- **Web Layer**: Controllers, API models, Authentication middleware

This repository shows pragmatic usages of DDD where appropriate (aggregate roots, repository interfaces) and keeps controllers thin.

## Folder Structure

```
src/
  ├─ Core/                # Domain entities, enums, interfaces
  ├─ Application/         # DTOs, Commands, Queries, Handlers, Profiles (AutoMapper)
  ├─ Infra/               # EF Core, Repositories, Migrations, Tenant providers
  ├─ Web/                 # ASP.NET Core Web API (Controllers, Middlewares)
  ├─ Tests/               # Unit and integration tests

docs/                    # Architecture docs, sequence diagrams
docker-compose.yml       # Quickstart services (Postgres, etc.)
README.md
```

## Tech Stack

- Language: C# 12 (on .NET 8.0)
- Frameworks: ASP.NET Core, Entity Framework Core
- Patterns: DDD, CQRS, MediatR, Clean Architecture
- Database: PostgreSQL
- Authentication: ASP.NET Core Identity + JWT (Access + Refresh tokens)
- Optional: Redis for distributed cache / refresh token store
- Tooling: dotnet CLI, EF Core CLI, Docker

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker & Docker Compose (recommended)
- Git

### Configuration

Copy the example environment file and edit values:

```bash
cp .env.example .env
```

Key environment variables:

- `POSTGRES_USER` - Postgres user
- `POSTGRES_PASSWORD` - Postgres password
- `POSTGRES_DB` - Database name
- `ASPNETCORE_ENVIRONMENT` - `Development` | `Production`
- `JWT_ISSUER` - JWT issuer
- `JWT_AUDIENCE` - JWT audience
- `JWT_SECRET` - Strong secret used to sign tokens (or use RSA keys in production)
- `TENANT_DEFAULT` - Default tenant identifier (when applicable)

`appsettings.Development.json` includes placeholders for the connection string and other settings. In production, keep secrets in secret stores.

### Run with Docker (recommended)

A `docker-compose.yml` is provided to launch Postgres (and Redis if enabled). Example quickstart:

```bash
# Start DB
docker-compose up -d

# Create DB and run migrations
dotnet tool restore
dotnet ef database update --project src/Infra/Infra.csproj

# Run the API
dotnet run --project src/Web/Web.csproj --launch-profile Docker
```

### Run locally

1. Restore packages

```bash
dotnet restore
```

2. Update database

```bash
dotnet ef database update --project src/Infra/Infra.csproj
```

3. Run the API

```bash
dotnet run --project src/Web/Web.csproj
```

Open `https://localhost:5001`.

## API Usage Examples

**Register**

```bash
curl -X POST https://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@example.com","password":"P@ssw0rd!"}'
```

**Login**

```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@example.com","password":"P@ssw0rd!"}'
```

Response contains `accessToken`, `refreshToken`, `expiresIn` and `user` info.

**Refresh token**

```bash
curl -X POST https://localhost:5001/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<your-refresh-token>"}'
```

**Tenant-aware request**

Include a header `X-Tenant-ID: tenant_abc` or use the host-based strategy depending on configuration.

## Deployment Guidance

- Use managed Postgres (Cloud SQL, RDS, Azure Database) for production.
- Store secrets (JWT keys, DB credentials) in a secrets manager (Azure Key Vault, AWS Secrets Manager).
- Use a reverse proxy (NGINX) or managed gateway and enable HTTPS (TLS termination).
- Consider rotating refresh tokens and storing them server-side in Redis for easy revocation.
- Enable logging (Serilog) and metrics (Prometheus) if needed.

## Security Notes

- **Never** commit `JWT_SECRET` or production connection strings to source control.
- Prefer asymmetric keys (RSA) for signing tokens in production.
- Enforce strong password policies via Identity options.
- Revoke refresh tokens on password change or suspicious events.

## Contributing

This repository is intended as a personal portfolio. If you want to adapt it for your own use:

1. Fork the project
2. Create a feature branch
3. Open a PR with a clear description

If you reproduce or copy portions of the code, please keep attribution in the header comments.

## License

This project is released under the **MIT License**. See [LICENSE](LICENSE) for details.

## Contact

Created by: **Your Name**  
Portfolio: `https://your-portfolio.example`  
Twitter: `@your_twitter_handle`

---
