# TODO

## Backlog
- Feature 11 [feat/11_sql_data_access] Replace MongoDB data access with SQL Server (EF Core, `Microsoft.EntityFrameworkCore.SqlServer`).
  First inspect the restored schema, then scaffold the DbContext/entities from it (database-first, no migrations).
  In Core: delete `MongoRepository` and `MongoDbSettings`, rewrite `TransactionRepository` on the DbContext, add `SqlServerSettings`.
  In Desktop: rewire DI in `App.xaml.cs` and `appsettings.json` (same PR, so the solution keeps building).
  Update `SkeletonTests` and package references.
- Feature 12 [feat/12_remove_mongodb] Remove MongoDB leftovers: `MongoDB.Driver` version in `Directory.Packages.props`
  (only after Feature 11) and `mongo-data/` in `.gitignore`. Compose service and `.env.example` are already cleaned.

## Done
- Fix 14 [fix/14_sql_server_coredump] Stop SQL Server crash dumps from filling the disk: dumps off, restart capped at 3 tries, 22.04 base image.
- Feature 10 [docs/10_sql_server_decision] Database decision: SQL Server (the old site already uses it). README updated, MongoDB migration doc removed, replacement split into Features 11–12.
- Feature 8 [feat/08_github_workflow] Add GitHub workflow for Build and Test the app.
- Feature 3 [docs/03_db_backup] One-time migration dump doc (removed later: the old database is SQL Server, not MongoDB).
- Feature 1 [docs/01_desktop_tech] Decide the technology to use for the desktop app: WPF + .NET 10 LTS, CommunityToolkit.Mvvm, MongoDB.Driver, NUnit + Moq.
- Feature 2 [docs/02_db_location] Decide where to keep the database for the first release: local MongoDB 8 in Docker with a `./mongo-data` bind mount; README explains the desktop-first porting path to the dockerized web app + API for mobile.
- Feature 4 [feat/04_skeleton] Scaffold the solution: Core + NUnit tests + Docker MongoDB (WPF shell moved to Feature 5).
- Feature 5 [feat/05_wpf_shell] WPF app shell with DI and MongoDB wiring.
- Feature 7 [feat/07_docker_compose] Prepare docker-compose for local MongoDB with bind-mounted data volume and configurable path via .env.
- Feature 9 [docs/09_sql_server_backup_restore] Doc for backup of the VPS SQL Server and restore into a local SQL Server container.
