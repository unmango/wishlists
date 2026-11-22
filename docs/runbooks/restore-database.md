# Restore the wishlists database

The live database is the CloudNativePG cluster `postgres` in the `wishlists` namespace on rosequartz. Two copies of it exist:

| Copy                                                       | Where                                                                    | What it gives you               | Rehearsed                                                        |
| ---------------------------------------------------------- | ------------------------------------------------------------------------ | ------------------------------- | ---------------------------------------------------------------- |
| Barman Cloud (WAL + daily base backup at 00:30 UTC)        | Ceph bucket `wishlists-postgres-backups`, ObjectStore `postgres-backups` | Point-in-time recovery, 60 days | 2026-11-22: 12 min, every row count matched                      |
| Velero `backblaze-daily` (CSI snapshot moved to Backblaze) | BackupStorageLocation `backblaze`, namespace `velero-system`             | Yesterday's volume, off site    | 2026-11-22: 4 min to restore the volume, every row count matched |

Use the Barman copy first. Use Velero only when the Ceph bucket is gone too.

## Don't peek

The maintainer is also an owner, and the database holds every bought mark. To check a restore, compare row counts and sign in to the app. Never select the contents of `mt_doc_purchasesview` or of `ItemMarkedBought` events.

Run this count on the live database before you start, if it still answers, and on the restored one after:

```sh
kubectl -n wishlists exec <pod> -c postgres -- psql -d wishlists -At -c "
  select (select count(*) from mt_events),
         (select max(seq_id) from mt_events),
         (select count(*) from mt_streams),
         (select count(*) from mt_doc_memberview),
         (select count(*) from mt_doc_wishlistview),
         (select count(*) from mt_doc_purchasesview),
         (select count(*) from mt_doc_dataprotectionkey)"
```

`mt_doc_dataprotectionkey` must hold at least one row. Those keys encrypt the sign-in cookies, so a restore that loses them signs everyone out.

The app rebuilds every view from `mt_events` on startup (ADR 0001), so the event counts matter most.

## Rehearse without touching the live database

Restore into a side cluster. It reads the bucket and never writes to it, because it has no WAL archiver.

```yaml
apiVersion: postgresql.cnpg.io/v1
kind: Cluster
metadata:
  name: postgres-drill
  namespace: wishlists
spec:
  instances: 1
  imageName: ghcr.io/cloudnative-pg/postgresql:18.6-system-trixie # match the live cluster
  enableSuperuserAccess: false
  storage:
    size: 5Gi
    storageClass: fast-rwo
  bootstrap:
    recovery:
      source: origin
      database: wishlists
      owner: wishlists
      # recoveryTarget:
      #   targetTime: "2026-11-22 06:08:00+00"
  externalClusters:
    - name: origin
      plugin:
        name: barman-cloud.cloudnative-pg.io
        parameters:
          barmanObjectName: postgres-backups
          serverName: postgres-r1 # the live cluster's archive folder
```

Run the count in `postgres-drill-1`, then `kubectl -n wishlists delete cluster postgres-drill` (its volume uses the `Delete` reclaim policy).

## Restore in place from Ceph (point in time)

The app connects to `postgres-rw` with a client certificate, so the restored cluster must keep the name `postgres` and the same certificate secrets.

The archive folder the live cluster writes to is the `serverName` in its plugin parameters (`postgres-r1` since the 2026-11-22 rehearsal). Recover from that folder, and archive into the next one.

1. Stop Flux from recreating the old cluster or the app: `flux -n flux-system suspend kustomization apps-wishlists` and `flux -n wishlists suspend helmrelease wishlists`.
2. If the old cluster still answers, take a base backup now (step 9's command) and wait for it to complete. The restore then replays minutes of WAL instead of a day's.
3. Stop the app: `kubectl -n wishlists scale deploy/wishlists --replicas 0`.
4. Delete the broken cluster: `kubectl -n wishlists delete cluster postgres`. Its volume goes with it. The certificate secrets (`postgres-pki-ca`, `postgres-server-cert`, `postgres-replication-cert`) and the ObjectStore stay.
5. Apply the live manifest from the-cluster with two changes:
   - Point `externalClusters` at the folder the old cluster archived into (`serverName: postgres-r1`), as in the drill above.
   - Point the WAL archiver at the next folder (`serverName: postgres-r2`, ...), so the new timeline never mixes with the old one. CNPG refuses a folder that already holds WAL.
6. Wait for `kubectl -n wishlists get cluster postgres` to report `Cluster in healthy state`, then run the count. If the recovery pod sits in `Init` for minutes, its node's container runtime is probably sick: delete the cluster, force-delete the stuck pod, and apply it again with a `spec.affinity.nodeAffinity` rule that excludes that node.
7. Start the app: `kubectl -n wishlists scale deploy/wishlists --replicas 1`. Sign in on the site. An existing session that still works proves the data protection keys came back.
8. Commit the same manifest change to the-cluster (`apps/wishlists`), then resume the Kustomization and the HelmRelease. Flux must not put the old `serverName` back.
9. Take a fresh base backup (`kubectl cnpg backup postgres -m plugin --plugin-name barman-cloud.cloudnative-pg.io`), so the next recovery has a short WAL replay.

## Restore from Backblaze (last resort)

Velero restores the volume into a spare namespace. Postgres then starts on it as after a power cut.

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: wishlists-drill
---
apiVersion: velero.io/v1
kind: Restore
metadata:
  name: wishlists-restore
  namespace: velero-system
spec:
  backupName: backblaze-daily-<timestamp> # velero -n velero-system backup get
  includedNamespaces: [wishlists]
  includedResources: [persistentvolumeclaims, persistentvolumes]
  namespaceMapping:
    wishlists: wishlists-drill
  restorePVs: true
```

Then mount `wishlists-drill/postgres-1` in a pod running the same Postgres image as uid 26, and start Postgres with a config of its own, because the CNPG config points at certificates the pod lacks:

```sh
D=/var/lib/postgresql/data/pgdata
chmod 0700 $D # fsGroup leaves the folder group-writable, and Postgres refuses to start on that
rm -f $D/postmaster.pid
printf 'local all all trust\n' > /tmp/hba
: > /tmp/ident
cat > /tmp/pg.conf <<EOF
data_directory = '$D'
hba_file = '/tmp/hba'
ident_file = '/tmp/ident'
listen_addresses = ''
unix_socket_directories = '/tmp'
archive_mode = off
EOF
postgres -c config_file=/tmp/pg.conf &
```

Run the count with `psql -h /tmp -U postgres -d wishlists`. To bring the data back, `pg_dump -h /tmp -U postgres -Fc wishlists` from that pod, then create a fresh `postgres` cluster (steps 1 to 5 above with `initdb`) and `pg_restore` the dump into it as the `wishlists` owner. Delete `wishlists-drill` afterwards.

## Timings from the 2026-11-22 rehearsal

- **Ceph point in time, side cluster:** 12 minutes from apply to Ready. Most of it was WAL replay: 69 segments since the 00:30 base backup, at about 10 seconds each. `archive_timeout: 5min` closes a segment every 5 minutes, so a restore late in the day replays up to about 290 segments, roughly 50 minutes.
- **Backblaze, side namespace:** the Velero restore finished in 4 minutes. Ceph timed out the first mount of the new volume and retried it after 2 minutes. Postgres recovered from the crash-consistent snapshot in under a second.
- **Ceph point in time, in place:** with a base backup taken just before, the restored `postgres` cluster reached Ready 2 minutes 48 seconds after the apply. The first attempt landed on a node whose container runtime kept timing out and sat in `Init` for 17 minutes; pinned to another node, it recovered at once. The site was down for 23 minutes in total, and about 6 without the sick node.
- Every restore matched the live row counts exactly, including the data protection key.
