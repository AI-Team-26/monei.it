# monei.it

Replacement of the old monei.it website.

Initially it will only have a Desktop app (.Net, C#) and later a website.

The development will be done on a restored MongoDB database.

## Tech stack

- **Desktop app:** WPF, C# on .NET 10 LTS (upgrade to .NET 11 when it reaches GA)
- **MVVM:** CommunityToolkit.Mvvm
- **Data access:** MongoDB.Driver with the repository pattern
- **Tests:** NUnit + Moq
- **Development database:** MongoDB in Docker, data kept on the host via a bind-mounted volume (`./mongo-data`)
- **Packaging:** self-contained single-file `.exe`
- **UI language:** English

## Development database (MongoDB)

```bash
docker compose up -d
```

Connection string, note the `authSource=admin` (the root user created by `MONGO_INITDB_ROOT_*` lives in the `admin` database):

```text
mongodb://monei:monei-dev-password@localhost:27017/monei?authSource=admin
```
