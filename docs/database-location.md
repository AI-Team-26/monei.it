# Where the database lives (first release)

## Decision

The database for the first release is a **local MongoDB 8 container**, with its data
files stored on the host machine through a bind mount (`./mongo-data`).

```yaml
services:
  mongodb:
    image: mongo:8.0
    ports:
      - "27017:27017"
    volumes:
      - ./mongo-data:/data/db
```

Connection string used by the desktop app during development:

```
mongodb://monei:monei-dev-password@localhost:27017/?authSource=admin
```

Those credentials are **development only**: the container listens on `localhost` and
is never exposed outside the development machine.

## Why this option

- **Same engine as the target.** The old application already stores its data as MongoDB
  documents, so no mapping or migration of the data model is needed.
- **The data outlives the container.** Because `/data/db` is bind-mounted, `docker compose
  down`, image updates and container recreation never touch the database files.
- **Inspectable and backup-friendly.** The whole database is a folder on the host, easy to
  copy, archive and restore without extra tooling or accounts.
- **No cloud dependency while porting.** Porting the app does not require an account,
  network access or a paid tier.
- **Same shape later on.** The web/API release runs the same MongoDB image in Docker, so
  moving from the laptop to a server is a change of host, not a change of technology.

## Options considered and rejected

| Option | Why not for the first release |
| --- | --- |
| Embedded database (SQLite / LiteDB) | Throwaway work: the existing data is document-oriented MongoDB and would have to be converted twice. |
| MongoDB Atlas (cloud) | Adds an account, network latency and a cost line before the app does anything. |
| MongoDB installed on the old Windows Server | The point of the rewrite is to leave that machine behind. |
| Files on a Docker named volume | Works, but hides the data from the host: copying and inspecting it needs extra commands, and the bind mount is just as safe. |

## Consequences

- Developers need Docker running locally before starting the app.
- `./mongo-data` is git-ignored and must never be committed.
- Backup and restore are a copy of `./mongo-data` (planned separately).
- When the web application and its API for the mobile app arrive, the same container is
  deployed next to the .NET service instead of next to the desktop app.
