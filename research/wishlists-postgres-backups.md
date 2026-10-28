# Backing up the wishlists Postgres (unmango/wishlists#459)

Question: how should the wishlists CloudNativePG `Cluster` (`apps/wishlists/postgres-cluster.yml` in the-cluster) be backed up with the Ceph S3 (Rook RGW) and Velero the cluster already runs? The database holds everything: the Marten event store, its projections and the ASP.NET data protection keys. It has to survive until at least Christmas 2026.

Researched 2026-10-28 against the-cluster at `ce6633d` and the upstream docs linked inline.

## Recommendation

Use both, with different jobs:

1. **Primary: CNPG native backups through the Barman Cloud Plugin**, writing base backups and continuous WAL to a new RGW bucket claimed in the `wishlists` namespace. This gives point-in-time recovery with an RPO of minutes, and the restore path is the one CNPG documents.
2. **Secondary, off-site: opt the CNPG volume into the existing Velero `backblaze-daily` schedule** by labelling it. This reuses the Backblaze location and schedule that already exist. It is crash-consistent only, but it is the only copy that leaves the house, and Postgres recovers from a crash-consistent image the same way it recovers from a power cut.

Don't use the in-tree `spec.backup.barmanObjectStore`: it has been deprecated since CNPG 1.26 and the cluster runs 1.30.1. Don't use Velero file-system backup (kopia over the live volume): it is not point-in-time.

## Facts from the-cluster

| What | Where | Finding |
| --- | --- | --- |
| CNPG operator | `infrastructure/controllers/cnpg-system/helm-release.yml` | chart `cloudnative-pg` 0.29.1, image tag **1.30.1**. No barman-cloud plugin installed. |
| Existing CNPG backups | `grep -ri 'barman\|ScheduledBackup\|backup:' apps/*/postgres-cluster.yml` | **None.** wishlists, harbor, bitwarden, gitea, keycloak and nix-system all run CNPG clusters with no `backup`, `plugins` or `ScheduledBackup`. The only DB backup procedure is a manual `pg_dump` in `docs/ncps.md`. This would be the first CNPG cluster with native backups. |
| cert-manager | `infrastructure/controllers/cert-manager-system` | Installed, which the plugin requires. |
| Velero | `infrastructure/controllers/velero-system/helm-release.yml` | Velero v1.18.4, aws plugin v1.14.4, node agent on zeus and gaea (`disableHostPath: true`). |
| Velero locations | `infrastructure/configs/velero-system/backup-storage-location.yml`, `volume-snapshot-location.yml` | `thecluster` (Ceph RGW bucket `velero`, `http://rook-ceph-rgw-s3.rook-ceph.svc`, default) and `backblaze` (B2 bucket `thecluster-velero`, us-west-002). |
| Velero schedules | `infrastructure/configs/velero-system/schedules.yml` | `{thecluster,backblaze}-{daily,weekly}`, all `snapshotVolumes: true`, `snapshotMoveData: true`, `datamover: velero`. Daily keeps 7 days, weekly keeps 4 weeks. They select resources by label: `backup.thecluster.io/schedule: daily|weekly` plus `backup.thecluster.io/thecluster: 'true'` or `backup.thecluster.io/backblaze: 'true'`. |
| Who opts in | `apps/seedvault/pvc.yml`, `apps/migrate/pvcs.yml` | Plain PVCs carrying those labels. Nothing in `apps/wishlists` carries one. |
| CSI snapshots | `infrastructure/configs/rook-ceph/snapshots/volume-snapshot-class.yml` | `VolumeSnapshotClass ceph-rbd`, annotated `velero.io/csi-volumesnapshot-class: "true"`. |
| Object store | `infrastructure/configs/rook-ceph/storage/ceph-object-store.yml` | `CephObjectStore s3`: metadata replicated 3 on SSD, data EC 2+1 on HDD with an **osd** failure domain. Gateway on port 80 (http) and 443 (cert `rgw-s3-tls` from ClusterIssuer `thecluster.lan`). |
| Bucket classes | `.../ceph-object-store-storage-class.yml`, `docs/storage.md` | `bucket` (current) and `ceph-bucket` (deprecated). Both have **`reclaimPolicy: Delete`**. |
| OBC pattern | `apps/harbor-system/object-bucket-claim.yml`, `apps/nix-system/object-bucket-claim.yml` | An OBC in the app's namespace. Rook answers it with a Secret and a ConfigMap of the same name, and the app reads `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`. harbor and pds use `http://rook-ceph-rgw-s3.rook-ceph.svc:80`. ncps uses `https://...:443`. |
| wishlists volume | `apps/wishlists/postgres-cluster.yml` | `fast-rwo`, 5Gi, one instance. Per `docs/storage.md`, `fast-rwo` is the `default-ssd` pool, replicated 2 with an **osd** failure domain because every SSD is in zeus. Both copies live on one host. |

Two consequences:

- **Losing zeus loses the database today.** Both RBD replicas are on zeus. The RGW data pool is EC 2+1 with an osd failure domain across zeus and gaea, so a bucket on it survives losing zeus only by luck of chunk placement (see `docs/storage.md`, "Failure domains"). The Ceph bucket alone doesn't protect against losing a host or the cluster. That is why the Backblaze copy matters.
- **A bucket on the `bucket` class is deleted with its claim.** Rook's OBC docs say `reclaimPolicy: Delete` means "physically delete the bucket" when the OBC is deleted ([Rook: Object Bucket Claim](https://rook.github.io/docs/rook/latest/Storage-Configuration/Object-Storage-RGW/ceph-object-bucket-claim/)). If the OBC lives in `apps/wishlists`, pruning that kustomization or deleting the namespace would take the backups with it. The OBC needs a class with `reclaimPolicy: Retain`.

## Options

### A. In-tree `spec.backup.barmanObjectStore`: rejected

- CNPG 1.30 docs: "As of CloudNativePG 1.26, native Barman Cloud support is deprecated in favor of the Barman Cloud Plugin". It is "still the default for backward compatibility", and no removal date is given ([CNPG 1.30: Appendix B, backup on object stores](https://cloudnative-pg.io/docs/1.30/appendixes/backup_barmanobjectstore); [CNPG 1.30: Backup](https://cloudnative-pg.io/docs/1.30/backup)).
- Building new config on a deprecated API means migrating it later ([plugin migration guide](https://cloudnative-pg.io/plugin-barman-cloud/docs/migration/)), so start on the plugin.

### B. Barman Cloud Plugin: recommended primary

- Requirements: CNPG **1.26 or newer** (we run 1.30.1). **cert-manager** must be present (it is). The plugin is installed **in the operator's namespace** (`cnpg-system`), from the Helm chart `cnpg/plugin-barman-cloud` in the same `https://cloudnative-pg.github.io/charts` repo the cluster already uses. The docs list the current release as v0.15.1 ([plugin installation](https://cloudnative-pg.io/plugin-barman-cloud/docs/installation/)).
- Configuration: an `ObjectStore` (`barmancloud.cnpg.io/v1`) **in the same namespace as the Cluster**. Its `.spec.configuration` has the same shape as the old `barmanObjectStore` (`destinationPath`, `endpointURL`, `endpointCA`, `s3Credentials`, `wal`, `data`), and retention moves to `ObjectStore.spec.retentionPolicy`. The Cluster gets `spec.plugins: [{name: barman-cloud.cloudnative-pg.io, isWALArchiver: true, parameters: {barmanObjectName: ...}}]`, and adding it triggers a rolling update. `ObjectStore.spec.serverName` "must always be left empty". The server name is a plugin parameter on the Cluster ([plugin usage](https://cloudnative-pg.io/plugin-barman-cloud/docs/usage/), [migration](https://cloudnative-pg.io/plugin-barman-cloud/docs/migration/)).
- Scheduling: `ScheduledBackup` with `method: plugin` and `pluginConfiguration.name: barman-cloud.cloudnative-pg.io`. Its schedule is a **six-field cron with seconds first** (`"0 0 0 * * *"` = daily at midnight). It also takes `backupOwnerReference` (`none|self|cluster`) and `immediate: true` ([CNPG 1.30: Backup](https://cloudnative-pg.io/docs/1.30/backup)).
- Object-store backups are hot backups and need the WAL archive. "WAL archive alone is useless. Without a physical base backup, you cannot restore", so both the WAL archiver and a scheduled base backup are needed ([CNPG 1.30: Backup](https://cloudnative-pg.io/docs/1.30/backup)).
- S3-compatible endpoints: set `endpointURL`. For an HTTPS endpoint with a private CA, set `endpointCA` to a Secret holding `ca.crt`. Newer boto3 can raise an `x-amz-content-sha256` error against some S3-compatible stores, and the documented workaround is `AWS_REQUEST_CHECKSUM_CALCULATION=when_required` and `AWS_RESPONSE_CHECKSUM_VALIDATION=when_required` in `instanceSidecarConfiguration.env` on the ObjectStore ([plugin: object stores](https://cloudnative-pg.io/plugin-barman-cloud/docs/object_stores/)). The docs don't say whether RGW needs this. Add it only if the first backup fails with that error.
- **Endpoint choice:** use `http://rook-ceph-rgw-s3.rook-ceph.svc:80`, as harbor, pds and Velero already do. No CA plumbing is needed, and the traffic never leaves the cluster network. The 443 listener would need the `thecluster.lan` CA copied into `wishlists` as an `endpointCA` Secret.
- **Credentials:** an OBC in `wishlists` produces Secret `<obc-name>` with `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` ([Rook OBC](https://rook.github.io/docs/rook/latest/Storage-Configuration/Object-Storage-RGW/ceph-object-bucket-claim/)), and the ObjectStore's `s3Credentials` point straight at those keys. Nothing to seal, which matches the namespace's "no credential to seal" stance in `postgres-cluster.yml`.

### C. Velero backup of the namespace

- **CSI snapshot with data movement** (what the existing schedules do): Velero takes a CSI snapshot, and the node agent copies it to the BSL with kopia. The Velero docs rate this point-in-time but **"crash-consistent"**, not application-consistent ([Velero: CSI snapshot data movement](https://velero.io/docs/main/csi-snapshot-data-movement/)). The wishlists cluster has a single PGDATA volume (no separate `walStorage`), so one RBD snapshot captures data and `pg_wal` atomically. Postgres starts from it by replaying WAL, as after a power loss. That is acceptable as a last resort but gives no PITR, and the RPO is 24h.
- **File-system backup** (kopia over the live volume): "the data is not captured at the same point in time, so is less consistent" ([Velero: file system backup](https://velero.io/docs/main/file-system-backup/)). For Postgres that can mean a torn data directory. **Don't use it for this volume.**
- Velero pre/post exec hooks (`pre.hook.backup.velero.io/command`, ...) run before and after the item's snapshot ([Velero: backup hooks](https://velero.io/docs/main/backup-hooks/)). A `CHECKPOINT` pre-hook would only shorten crash recovery, so it isn't worth the complexity.
- CNPG's own docs don't mention Velero or third-party storage-level snapshots of a running cluster. Their snapshot story is CNPG-driven `method: volumeSnapshot`, which uses `pg_backup_start/stop` for hot snapshots or fences the instance for cold ones ([CNPG 1.30: volume snapshot backups](https://cloudnative-pg.io/docs/1.30/appendixes/backup_volumesnapshot)). That method keeps the snapshot inside Ceph, so it doesn't add an off-site copy and doesn't replace B.
- **Not verified:** how cleanly a Velero restore of a CNPG `Cluster`, its PVC and its operator-owned pod comes back. CNPG adopts PVCs by its own labels and annotations, and Velero restores those too, but no primary source documents this pairing. Treat the Velero copy as raw data to mount, not a one-command restore, and rehearse it once (see Restore, path 2).

### D. Both: chosen

B gives PITR and a documented restore path. C, through the existing Backblaze schedule, puts a daily copy outside the house at almost no cost: 5Gi, and only one label is needed. Neither alone covers both "I deleted a wishlist by mistake at 14:02" and "zeus died".

A `pg_dump` CronJob to the bucket would be a third, logical, version-independent copy. It is optional. Barman base backups depend on the same PostgreSQL major version, and a dump doesn't, but nothing here plans a major upgrade before Christmas.

## What to add to the-cluster

Not written into the-cluster, per the brief. Names are suggestions.

1. **`infrastructure/controllers/cnpg-system/plugin-barman-cloud.yml`**: a Flux `HelmRelease` for chart `plugin-barman-cloud` from the existing `HelmRepository cloudnative-pg`, `targetNamespace: cnpg-system`, version pinned (v0.15.1 per the docs at time of writing; let Renovate bump it). Add it to that directory's `kustomization.yaml`. The plugin's chart issues cert-manager `Certificate`s, so add `dependsOn: [{name: infra-cert-manager}]` to the `infra-cnpg-system` Flux Kustomization in `clusters/rosequartz/infrastructure.yaml`, which has no `dependsOn` today.
2. **A retained bucket class**, e.g. `bucket-retain` in `infrastructure/configs/rook-ceph/storage/ceph-object-store-storage-class.yml`: same provisioner and parameters as `bucket`, with `reclaimPolicy: Retain`. Document it in `docs/storage.md`'s class table ("object storage that must outlive its claim").
3. **`apps/wishlists/object-bucket-claim.yml`**: `ObjectBucketClaim postgres-backups`, `bucketName: wishlists-postgres-backups`, `storageClassName: bucket-retain`.
4. **`apps/wishlists/object-store.yml`**: `barmancloud.cnpg.io/v1 ObjectStore postgres-backups` with
   - `configuration.destinationPath: s3://wishlists-postgres-backups/`
   - `configuration.endpointURL: http://rook-ceph-rgw-s3.rook-ceph.svc:80`
   - `configuration.s3Credentials.accessKeyId: {name: postgres-backups, key: AWS_ACCESS_KEY_ID}` and `secretAccessKey: {name: postgres-backups, key: AWS_SECRET_ACCESS_KEY}`
   - `configuration.wal.compression: gzip`, `configuration.data.compression: gzip`
   - `retentionPolicy: "60d"`. Tiny data, so keep plenty, and make sure it spans any gap between noticing a problem and acting.
5. **`apps/wishlists/postgres-cluster.yml`** edits:
   - `spec.plugins: [{name: barman-cloud.cloudnative-pg.io, isWALArchiver: true, parameters: {barmanObjectName: postgres-backups}}]` (rolling restart of the single instance means a few seconds of downtime).
   - `spec.inheritedMetadata.labels: {backup.thecluster.io/schedule: daily, backup.thecluster.io/backblaze: 'true'}` so the PVC (and the other CNPG objects) join the existing `backblaze-daily` Velero schedule. Add `backup.thecluster.io/thecluster: 'true'` only if an in-cluster Velero copy is also wanted. It is mostly redundant with B.
6. **`apps/wishlists/scheduled-backup.yml`**: `ScheduledBackup postgres-daily`, `cluster.name: postgres`, `schedule: "0 30 0 * * *"` (00:30 daily, seconds first, before Velero's 00:00 Backblaze run the next day), `method: plugin`, `pluginConfiguration.name: barman-cloud.cloudnative-pg.io`, `backupOwnerReference: self`, `immediate: true` so the first base backup exists as soon as it lands.
7. **`apps/wishlists/kustomization.yaml`**: add the three new files. Ordering is already covered: `apps-wishlists` in `clusters/rosequartz/apps.yaml` already `dependsOn` `infra-cnpg-system` (which will ship the ObjectStore CRD via step 1) and `infra-configs-rook-ceph-storage` (which will ship `bucket-retain` via step 2). Update the comments on those edges to mention the new reasons.

Checks after rollout:
- `kubectl -n wishlists get cluster postgres -o jsonpath='{.status.conditions}'` shows `ContinuousArchiving=True`.
- `kubectl -n wishlists get backup` shows a `completed` backup from `immediate`.
- `velero backup describe <backblaze-daily-...> --details` lists the `postgres-1` PVC with a completed DataUpload.

## How a restore works

### Path 1: PITR from the RGW bucket (normal case)

Per [CNPG 1.30: Recovery](https://cloudnative-pg.io/docs/1.30/recovery) and the [plugin usage docs](https://cloudnative-pg.io/plugin-barman-cloud/docs/usage/):

1. Suspend Flux for the wishlists kustomization, scale the app's Deployment to 0, and delete `Cluster postgres`. Its PVC goes with it (`fast-rwo` is `Delete`). Take an ad-hoc Velero backup of the namespace first if the volume still holds anything worth keeping. The OBC and bucket are untouched (`Retain`, separate object).
2. Edit `postgres-cluster.yml`. Replace `bootstrap.initdb` with:
   ```yaml
   bootstrap:
     recovery:
       source: origin
       recoveryTarget:            # omit to replay to the end of the archive
         targetTime: "2026-12-20 14:00:00+00"
   externalClusters:
     - name: origin
       plugin:
         name: barman-cloud.cloudnative-pg.io
         parameters:
           barmanObjectName: postgres-backups
           serverName: postgres          # the archive the old cluster wrote
   plugins:
     - name: barman-cloud.cloudnative-pg.io
       isWALArchiver: true
       parameters:
         barmanObjectName: postgres-backups
         serverName: postgres-r1         # new archive path; must differ
   ```
   The new archive needs a different `serverName`. CNPG's empty-archive safety check otherwise leaves the cluster stuck in `Setting up primary` with `Expected empty archive`. Don't use `cnpg.io/skipEmptyWalArchiveCheck` ([CNPG 1.30: Recovery](https://cloudnative-pg.io/docs/1.30/recovery)). Timestamps without an offset are read as UTC.
3. Keep the Cluster name `postgres` so the `postgres-rw` Service and the client certificates in `certificate.yml` (issued for that name and held in cert-manager Secrets) keep working. The `wishlists` role and database come back from the backup. The data protection keys are in the database, so existing session cookies stay valid.
4. Resume Flux, wait for `Cluster postgres` to report healthy, then scale the app back up.
5. Later, a cleanup PR can drop `bootstrap.recovery` and `externalClusters` from the manifest. CNPG only reads `bootstrap` at creation. Leave the new `serverName: postgres-r1`.

### Path 2: from Velero/Backblaze (zeus or the whole Ceph cluster lost)

1. With the cluster rebuilt and Velero pointed at the `backblaze` BSL, restore only the PVC from the latest `backblaze-daily` backup into `wishlists`: `velero restore create --from-backup <name> --include-namespaces wishlists --include-resources persistentvolumeclaims,persistentvolumes`.
2. Re-create `Cluster postgres` from Git with its normal `initdb` bootstrap removed, adopting the restored PVC. Alternatively, bring up a throwaway pod that mounts the PVC, run `pg_dump`, and `bootstrap.initdb.import` (or `psql`) into a fresh cluster. The dump route is slower but doesn't depend on CNPG adopting a foreign PVC. Postgres starts by crash recovery and loses up to ~24h.
3. Rehearse this once before Christmas into a scratch namespace (with `--namespace-mappings wishlists:wishlists-restore-test`). It is the step no primary source covers end to end.

## Open points

- RGW and the boto3 checksum change: untested here. If the first WAL push logs `x-amz-content-sha256`, add the two `AWS_*_CHECKSUM_*` env vars to `ObjectStore.spec.instanceSidecarConfiguration.env`.
- Plugin chart version: the docs show v0.15.1. Pin whatever the chart index shows on the day and let Renovate follow.
- Whether the plugin chart needs any values beyond defaults on this cluster (the install docs list none).
