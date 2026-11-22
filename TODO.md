# TODO

## Backlog
- Feature 12 [feat/12_desktop_sql_wiring] Point the desktop app at SQL Server and remove the Mongo code:
  DI in `App.xaml.cs`, connection string in `appsettings.json` (`Server=localhost,1433;Database=monei;...`),
  delete `MongoRepository` and `MongoDbSettings`. Needs Feature 11.2.
- Feature 13 [feat/13_remove_mongodb] Remove MongoDB leftovers: `MongoDB.Driver` version in `Directory.Packages.props`
  (only after Feature 12) and `mongo-data/` in `.gitignore`. Compose service and `.env.example` are already cleaned.
- Bug 16 [fix/16_plaintext_passwords] `User.Password` and `Account.Password` hold plain text passwords.
  Choose the hashing and the migration path before go-live.

## Done
- Feature 11.1 [feat/11.1_ef_core_package] `Microsoft.EntityFrameworkCore.SqlServer` added to Core, with a pinned `System.Security.Cryptography.Pkcs` because `Microsoft.Data.SqlClient` pulls a vulnerable version.
- Feature 10 [docs/10_sql_server_decision] Database decision: SQL Server (the old site already uses it). README updated, MongoDB migration doc removed, replacement split into Features 11–12.
- Feature 8 [feat/08_github_workflow] Add GitHub workflow for Build and Test the app.
- Feature 3 [docs/03_db_backup] One-time migration dump doc (removed later: the old database is SQL Server, not MongoDB).
- Feature 1 [docs/01_desktop_tech] Decide the technology to use for the desktop app: WPF + .NET 10 LTS, CommunityToolkit.Mvvm, MongoDB.Driver, NUnit + Moq.
- Feature 2 [docs/02_db_location] Decide where to keep the database for the first release: local MongoDB 8 in Docker with a `./mongo-data` bind mount; README explains the desktop-first porting path to the dockerized web app + API for mobile.
- Feature 4 [feat/04_skeleton] Scaffold the solution: Core + NUnit tests + Docker MongoDB (WPF shell moved to Feature 5).
- Feature 5 [feat/05_wpf_shell] WPF app shell with DI and MongoDB wiring.
- Feature 7 [feat/07_docker_compose] Prepare docker-compose for local MongoDB with bind-mounted data volume and configurable path via .env.
- Feature 9 [docs/09_sql_server_backup_restore] Doc for backup of the VPS SQL Server and restore into a local SQL Server container.
