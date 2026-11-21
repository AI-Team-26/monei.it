# Back up SQL Server on the VPS and restore it in Docker

The old monei.it website keeps its data in **SQL Server** on a Windows VPS. To work with real
data locally, we take a `.bak` backup on the VPS, copy it to the PC and restore it into
**SQL Server 2022 running in Docker** on the PC.

SQL Server Management Studio **18.1** is installed on the VPS, so the backup can be done by
mouse. Every step also has a T-SQL version, for the cases where the mouse is not enough.

This is a **manual copy**, not a scheduled backup. We do it twice:

1. **Now:** to develop the app against real data.
2. **At go-live:** stop the old site, take a fresh backup, restore it again.

## 0. What you need

- VPS: SSMS 18.1, a login that can back up the database, free disk space for one `.bak`.
- PC: Docker with at least **2 GB RAM free** (a SQL Server container will not start below that).
- PC: SSMS, to connect to the container.
- The `.bak` file, copied from the VPS to the PC. Keep it outside git: the default folder is
  `./sql-backup`, which is git-ignored.

## 1. Facts to collect on the VPS

**Version and edition.** In SSMS, right-click the server → **Properties** → **General**, or run:

```sql
SELECT @@VERSION, SERVERPROPERTY('Edition') AS Edition, SERVERPROPERTY('ProductMajorVersion') AS MajorVersion;
```

**Database name and size.** `sp_helpdb`, or right-click the database → **Properties** → **General**.

**The restore rule:** a backup can be restored into the **same or a newer** SQL Server version,
never into an older one. If the VPS runs an older version than the container, the restore fails.
The container uses SQL Server 2022, so a 2012 or newer backup restores fine. For an older VPS,
change the image tag in `docker-compose.yml`.

**Backup compression** is not available in SQL Server **Express**. The edition query above tells
you: drop `COMPRESSION` when the VPS runs Express.

## 2. Take the backup (SSMS 18.1)

In SSMS Object Explorer on the VPS:

1. **Databases** → right-click the monei database → **Tasks** → **Back Up…**
2. **Backup type:** Full.
3. **Destination:** remove the suggested path, then **Add…** → `C:\backups\monei.bak`.
4. **Media options:** *Overwrite all existing backup sets*.
5. **Backup options:** tick **Verify backup when finished** and **Perform checksum before writing
   to media**. Tick **Compress backup** too, but not on Express.
6. **OK**.

Same backup in T-SQL:

```sql
BACKUP DATABASE [monei]
TO DISK = N'C:\backups\monei.bak'
WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;
```

Two traps:

- The path is on the **VPS**, written by the **SQL Server service account**, not by your Windows
  user. `C:\backups` must exist and that account must be allowed to write into it. When in doubt,
  keep the default folder that the dialog proposes (under `...\MSSQL\Backup`).
- The backup is one file holding the full database. That is all we need for this copy.

When it finishes, **Messages** shows `BACKUP DATABASE successfully processed ... pages`. Check the
file size on disk.

## 3. Check the `.bak` before copying

Optional but cheap — it proves the file is complete:

```sql
RESTORE VERIFYONLY FROM DISK = N'C:\backups\monei.bak';
```

If the backup was written with checksums (both the SSMS box above and `WITH CHECKSUM` do that), the
stronger check is `RESTORE VERIFYONLY ... WITH CHECKSUM`: it fails when the backup has no checksums.

In SSMS the same check is inside **Restore Database…** → **Verify Backup Media**.

## 4. Copy the `.bak` to the PC

Over Remote Desktop: copy the file on the VPS and paste it into a folder on the PC (clipboard file
transfer). If local drives are shared in the RDP session, copy to `\\tsclient\C\...` instead — much
faster for big files.

Put it in the folder the container mounts, `./sql-backup` next to `docker-compose.yml`. To use
another folder, set `SQL_BACKUP_PATH` in `.env`.

## 5. Start SQL Server in Docker

```bash
docker compose up -d sqlserver
```

The `sqlserver` service in `docker-compose.yml`:

- image `mcr.microsoft.com/mssql/server:2022-latest`, Developer edition;
- port `127.0.0.1:1433` on the PC;
- `./sql-backup` bind-mounted at `/var/opt/mssql/backup`, so the `.bak` is visible to the server
  with no copy inside the container;
- the data folder bind-mounted at `/var/opt/mssql`, so the restored database survives container
  recreation;
- `restart: unless-stopped`: this is the real data for everyday use, so once started the container
  comes back by itself after a PC reboot. `docker compose stop sqlserver` stops it until you start it
  again;
- user `sa` with the local development password `monei-Dev-Password-1`.

Wait for the server to be ready:

```bash
docker compose logs -f sqlserver
```

Stop following when the log says `SQL Server is now ready for client connections`. It takes about
20 seconds on the first start.

Connect from SSMS on the PC:

- Server name: `localhost,1433` (comma, not colon)
- Authentication: SQL Server Authentication
- Login: `sa`, password: `monei-Dev-Password-1`
- Tick **Trust server certificate** (the container uses a self-signed certificate)

## 6. Restore into the container

The container runs **SQL Server on Linux**, and the `.bak` was written by a Windows server, so it
records Windows paths such as `C:\...\monei.mdf`. The Linux container cannot write there. The
restore must **move** the data and log file to a Linux path.

**Step 1 — read the logical file names inside the backup:**

```sql
RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/backup/monei.bak';
```

The result has two rows: one **D** (data) and one **L** (log). Note their `LogicalName`,
usually the database name and the database name plus `_log`.

**Step 2 — restore and move the files:**

```sql
RESTORE DATABASE [monei]
FROM DISK = N'/var/opt/mssql/backup/monei.bak'
WITH REPLACE,
     MOVE N'monei'      TO N'/var/opt/mssql/data/monei.mdf',
     MOVE N'monei_log'  TO N'/var/opt/mssql/data/monei_log.ldf',
     RECOVERY,
     STATS = 10;
```

Replace the two `LogicalName` values with the ones you saw in step 1.

Through SSMS instead of T-SQL: **Databases** → right-click → **Restore Database…** → **Device** →
**Add** → `/var/opt/mssql/backup/monei.bak` → open the **Files** page and set both **Restore As**
paths under `/var/opt/mssql/data`. If you skip the **Files** page, the restore fails with an
access or invalid path error.

**Without SSMS**, run the statements inside the container:

```bash
docker compose exec sqlserver \
  /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P monei-Dev-Password-1 \
  -Q "RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/backup/monei.bak'"
```

`-C` tells `sqlcmd` to trust the self-signed certificate. The same command shape runs the
`RESTORE DATABASE` statement.

## 7. Check the restore

```sql
SELECT name, state_desc FROM sys.databases;

DBCC CHECKDB (N'monei') WITH NO_INFOMSGS;
```

`state_desc` must be `ONLINE` and `DBCC CHECKDB` must end without errors. Then compare a few row
counts with the VPS, for example:

```sql
SELECT COUNT(*) FROM dbo.[name of a busy table];
```

## 8. The go-live copy

A full backup is a consistent copy of the database as of the moment it finishes. For the final
copy, take no chances:

1. Stop the old website on the VPS (or put the database in read-only) so nothing writes during the
   backup.
2. Take a new full backup with its own name, e.g. `C:\backups\monei-final.bak`.
3. Verify it, copy it to the PC, restore it with the same commands as above.
4. `REPLACE` overwrites the previous local copy, so repeat the same steps; no cleanup needed.

## Troubleshooting

| Message | Cause and fix |
| --- | --- |
| `Cannot open backup device ... Access is denied` | The SQL Server service account cannot write the backup folder. Use the default `...\MSSQL\Backup` folder or fix folder permissions. |
| `The backup set holds a backup of a database other than the existing ... database` | A database with that name already exists locally. Add `REPLACE`. |
| `The database cannot be restored ... because the database was backed up on a server running version X` | The container is **older** than the VPS. Use a newer image tag. |
| `Operating system error ... on file "C:\...\monei.mdf"` while restoring | The MOVE paths are missing or still Windows paths. Keep them under `/var/opt/mssql`. |
| `Login failed for user 'sa'` | Wrong password, or the container was started earlier with a different one. The password is set only when the data folder is empty: remove `./sql-data` and start again. |
| The container stops right after start | Not enough memory. SQL Server needs 2 GB; give Docker more memory or stop the MongoDB container meanwhile. |
| `The server must be started with permission ...` or a permission error on `/var/opt/mssql` right after start | Only on a Linux host: the bound data folder must belong to user `10001:10001`. Run `sudo chown -R 10001:10001 ./sql-data`. Windows hosts with Docker Desktop do not have this problem. |