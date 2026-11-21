# Move the database from the old website to the new app

The old monei.it website keeps its data in MongoDB on a Windows VPS. The new app uses
MongoDB 8 in Docker on the local PC. This document describes how to move the data.

This is a **one-time migration dump**, not a recurring backup. It happens twice:

1. **Now:** dump the live database, restore it locally, develop the app against real data.
2. **At cutover:** shut down the old website, take a fresh dump, restore it again.
   This second dump is the final data the app goes live with.

Only the database is moved. One dump file is kept, stored locally. No schedule, no rotation.

## 1. Checks on the VPS

Log in to the VPS (Remote Desktop) and open a bash shell. The database is not exposed
to the network, so every command below runs on the VPS itself.

**Is MongoDB running, and which version?**

```bash
mongosh --quiet --eval "db.version()"
```

If `mongosh` is missing, try the older `mongo` shell. Any version from 3.6 up dumps
fine and restores into MongoDB 8.

**Does the database require a login?**

If the command above works without credentials, there is no auth (common for old
localhost-only installs). If it fails with "Unauthorized", find the connection string
in the old website's config file (e.g. `web.config` or `appsettings.json`) and use it
in the dump command below.

**What is the database name and size?**

```bash
mongosh --quiet --eval "db.adminCommand('listDatabases').databases.forEach(d => print(d.name, d.sizeOnDisk))"
```

Note the name of the monei database (assumed `monei` below) and its size. The gzipped
archive is usually much smaller than `sizeOnDisk`.

**Is `mongodump` available?**

```bash
mongodump --version
```

Since MongoDB 4.4 the tools are a separate download. If missing, download the
"MongoDB Database Tools" Windows zip from
<https://www.mongodb.com/try/download/database-tools>, unzip it anywhere (no install
needed) and use the full path to `mongodump.exe`.

## 2. Take the dump

On the VPS, in bash:

```bash
mongodump --uri "mongodb://127.0.0.1:27017" --db monei --archive=monei-dump.gz --gzip
```

If the database requires a login, use the full connection string instead:

```bash
mongodump --uri "mongodb://USER:PASSWORD@127.0.0.1:27017/monei?authSource=admin" --archive=monei-dump.gz --gzip
```

This produces a single file, `monei-dump.gz`. Check that it exists and is not empty.

For the cutover dump: stop the old website first so nothing writes during the dump,
then run the same command. Keep the file name distinct, e.g. `monei-dump-final.gz`.

## 3. Copy the dump to the local PC

The simplest way with Remote Desktop: copy the file on the VPS and paste it into a
folder on the PC (RDP clipboard file transfer). If the RDP session has local drives
shared, copy it to `\\tsclient\C\...` instead — faster for large files.

Keep the dump outside the git repository (or in a git-ignored folder). Real data must
never be committed.

## 4. Restore into Docker MongoDB

The restore itself is **Feature 6**. In short, with the Docker database running
(`docker compose up -d`), from the folder holding the dump:

```bash
docker compose exec -T mongodb mongorestore \
  --username monei --password monei-dev-password --authenticationDatabase admin \
  --archive --gzip --drop < monei-dump.gz
```

`--drop` replaces the existing collections, so the same command works for both the
first restore and the final cutover restore. Feature 6 will verify and detail this step.
