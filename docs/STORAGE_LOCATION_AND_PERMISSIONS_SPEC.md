# Storage Location and Permissions Specification

## Location

Use a DraftRescue-owned per-user local application-data directory. Do not place recoverable drafts in Documents, Desktop, roaming profile, cloud-synchronized folders, or a shared machine-wide directory by default.

Conceptual layout:

```text
DraftRescue/
  data/
    drafts.db
    installation-secret.bin
  quarantine/
  logs/           # content-free only
  settings/
```

Exact Windows path resolution belongs to the Windows platform/application-host layer.

## Access model

MVP is a normal per-user desktop process. Do not broaden ACLs, use world-writable/shared directories, or install a privileged storage service.

## Path safety

- create directories deliberately;
- reject path traversal in any future diagnostic/export operation;
- do not follow arbitrary user-controlled symlinks/reparse points for the canonical store path without a security review;
- test concurrent first-start directory creation.

## Cloud sync

If environment redirection causes the resolved path to be cloud-synchronized, product behavior requires a future explicit policy. Do not intentionally choose roaming/cloud paths.
