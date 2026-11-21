# TODO

## Backlog
- Feature 3 [docs/03_db_backup] Plan the backup + restore of the MongoDB database.
- Feature 5 [feat/05_wpf_shell] WPF app shell with DI and MongoDB wiring.
- Feature 6 [feat/06_restore_dump] Restore the MongoDB dump into the Docker bind-mounted data folder.

## Done
- Feature 1 [docs/01_desktop_tech] Decide the technology to use for the desktop app: WPF + .NET 10 LTS, CommunityToolkit.Mvvm, MongoDB.Driver, NUnit + Moq.
- Feature 2 [docs/02_db_location] Decide where to keep the database for the first release: local MongoDB 8 in Docker with a `./mongo-data` bind mount; README explains the desktop-first porting path to the dockerized web app + API for mobile.
- Feature 4 [feat/04_skeleton] Scaffold the solution: Core + NUnit tests + Docker MongoDB (WPF shell moved to Feature 5).
- Feature 7 [feat/07_docker_compose] Prepare docker-compose for local MongoDB with bind-mounted data volume and configurable path via .env.
