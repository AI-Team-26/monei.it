# monei.it

Replacement of the old monei.it website.

The current application is an old **ASP.NET + Angular** site running on a **Windows Server**.
The goal is to replace it with a modern, **dockerized .NET application** that serves the web
UI **and exposes an API for a mobile app** used to insert and query the same data.

The **super-simple local desktop app is only the first step**: it lets us port the data and
the day-to-day workflows away from the old server, with the smallest possible surface, before
the web app, the API and the mobile app are built on top of the same data.

## Roadmap

1. **Desktop app** (now) — WPF on .NET 10, talks to SQL Server directly, single `.exe` for Windows.
2. **Web app + API** (next) — dockerized .NET service serving the web UI and exposing an API
   for the mobile app.
3. **Mobile app** (later) — consumes the same API.

## Tech stack

- **Desktop app:** WPF, C# on .NET 10 LTS (upgrade to .NET 11 when it reaches GA)
- **MVVM:** CommunityToolkit.Mvvm
- **Data access:** Microsoft.Data.SqlClient + Dapper with the repository pattern
- **Tests:** NUnit + Moq
- **Development database:** SQL Server 2022 in Docker, data kept on the host via a bind-mounted volume (`./sql-data`)
- **Packaging:** self-contained single-file `.exe`
- **UI language:** English

## Database

The database is **SQL Server**. The old website already keeps its data in SQL Server on the
VPS, so we keep the same database engine and the existing schema: no data conversion, no new
schema design.

The development database is **SQL Server 2022 running locally in Docker**. It is not embedded,
not in the cloud and not installed on the host: containers are the deployment unit we want for
the final dockerized application, so we use the same one from day one.

- Start it with `docker compose up -d sqlserver` (service definition in `docker-compose.yml`).
- Data lives in `./sql-data` (bind mount), so it survives container recreation and image updates.
- Connection string: `Server=localhost,1433;Database=monei;User Id=sa;Password=monei-Dev-Password-1;TrustServerCertificate=True`
  (local development credentials only).
- `./sql-data` and `./sql-backup` are git-ignored — never commit the database files.

`docs/sql-server-backup-restore.md` explains how to take a `.bak` backup on the VPS and restore
it into the local container, which keeps the real data on the PC.

The code still contains the MongoDB repository and wiring written before this decision.
Replacing it with SQL Server data access is tracked in `TODO.md` (Features 11 and 12).

The desktop app connects to this local database. `docker-compose.yml` is there to raise the
database on the local machine only: it is not a server deployment and nothing is hosted remotely.
