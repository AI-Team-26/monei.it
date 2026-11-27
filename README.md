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
- **Data access:** EF Core (`Microsoft.EntityFrameworkCore.SqlServer`) with the repository pattern.
  The entities are scaffolded from the existing database; the app never changes the schema by
  itself — schema changes are applied manually with the scripts in `docs/sql/`.
- **Tests:** NUnit + Moq
- **Development database:** SQL Server 2025 in Docker, data kept on the host via a bind-mounted volume (`./sql-data`)
- **Packaging:** self-contained single-file `.exe`
- **UI language:** English

## Database

The database is **SQL Server**. The old website already keeps its data in SQL Server on the
VPS, so we keep the same database engine and the existing schema: no data conversion, no new
schema design.

The development database is **SQL Server 2025 running locally in Docker**. It is not embedded,
not in the cloud and not installed on the host: containers are the deployment unit we want for
the final dockerized application, so we use the same one from day one.

- Start it with `docker compose up -d sqlserver` (service definition in `docker-compose.yml`).
- Data lives in `./sql-data` (bind mount), so it survives container recreation and image updates.
- Connection string: `Server=localhost,1433;Database=monei;User Id=sa;Password=monei-Local-Password-1;TrustServerCertificate=True`
  (local development credentials only).
- `./sql-data` and `./sql-backup` are git-ignored — never commit the database files.

`docs/sql-server-backup-restore.md` explains how to take a `.bak` backup on the VPS and restore
it into the local container, which keeps the real data on the PC.

Before running the app against an existing database (for example one restored from a VPS `.bak`),
run `docs/sql/password_migration.sql`. It widens `Account.Password`, blanks every stored password
(users must reset them until Feature 21 ships) and adds `Account.PasswordResetRequired`. Take a
backup first: blanking has no undo.

```bash
sqlcmd -S localhost,1433 -U sa -P monei-Local-Password-1 -d monei -b -i docs/sql/password_migration.sql
```
The `-b` flag makes `sqlcmd` exit non-zero on error; run it in SSMS works just as well.

The code still contains the MongoDB repository and wiring written before this decision.
Replacing it with SQL Server data access is tracked in `TODO.md` (Features 11 and 12).

The desktop app connects to this local database. `docker-compose.yml` is there to raise the
database on the local machine only: it is not a server deployment and nothing is hosted remotely.

### SQL Server crash loop (known upstream bug)

A known SQL Server 2025 bug ([mssql-docker#974](https://github.com/microsoft/mssql-docker/issues/974))
sometimes kills the container during startup, before it opens its errorlog. Symptoms:

- the container restarts again and again;
- `./sql-data/log` grows fast: a ~1.3 GB dump per crash, 30-40 GB in minutes;
- `docker compose logs` shows `[AppLoader] Failed to load LSA: 0xc0070102`.

What we do about it:

- `./sql-data/mssql.conf` turns the automatic dumps off with `[coredump] disablecoredump = true`.
  A crash then costs a few KB. The file must be saved **without BOM**, otherwise SQL Server
  ignores it (`head -c 3 ./sql-data/mssql.conf | od -An -tx1` must not print `ef bb bf`).  
  (You can copy it from _docs/mssql.conf_)
- Restarts are capped at three tries, so a loop stops by itself:
  `docker update --restart=on-failure:3 "$(docker compose ps -a -q sqlserver)"`.
  Compose recreates the container when the compose file changes, so re-apply this after a
  `down` / `up`.
- Delete dumps left by an old loop with `rm -rf ./sql-data/log/core.sqlservr.*`.

The image is pinned to the `ubuntu-22.04` base because the `ubuntu-24.04` one hits this bug more
often. Move back to 24.04 when SQL Server 2025 CU12 ships the fix.


### Bind mount: use WSL2

   **Bind mount on the Windows filesystem does not work.**
   SQL Server 2025 cannot start when `/var/opt/mssql` is a bind mount from a Windows drive:
   the LSA subsystem fails (`Failed to load LSA: 0xc0070102`) and the container crash-loops.
   See microsoft/mssql-docker#974.

   A bind mount from WSL2 works, because there the data sits on a real Linux filesystem (ext4).

   Assuming the WSL2 distribution is called "Ubuntu", this is the path as seen from Windows Explorer:

       __\\wsl.localhost\Ubuntu\Databases\monei__
  
In the WSL2 shell:  
```sh
sudo mkdir -p /Databases/monei
# 10001 = mssql inside the container (not 10002). Bind mounts pass numbers, not names.
# -R    = apply to the folder and everything inside it.
# 755   = owner reads/writes/enters; others may list and enter, but not write.
sudo chown -R 10001:10001 /Databases/monei
sudo chmod 755 /Databases/monei

# copy mssql.conf in /Databases/monei/mssql.conf
# sudo cp <project path>/docs/mssql.conf /Databases/monei/
# 644   = owner read/write; everyone else can read only
sudo chown 10001:10001 /Databases/monei/mssql.conf
sudo chmod 644 /Databases/monei/mssql.conf
```

For the backup file, once hte database is up and running:
```sh
sudo mkdir -p /Databases/monei/backup
sudo chown 10001:10001 /Databases/monei/backup

# copy the database backup file
# sudo cp <project path>/docs/monei.bak /Databases/monei/backup/
```

then folow the instruction in _docs/sql-server-backup-restore.md_.

