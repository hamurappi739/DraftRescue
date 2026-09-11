# Packaging, Installation, and Update Specification

**Status:** product/architecture baseline; final packaging ADR deferred until Windows validation.

## 1. Goals

Installation must be understandable, signed, reversible, privacy-preserving, and compatible with background startup. Updating DraftRescue must not expose drafts, silently change privacy defaults, or leave multiple observer processes running.

## 2. Packaging direction

Windows supports packaging classic desktop/.NET apps with MSIX. MSIX is the preferred direction to evaluate first because it provides package identity, signing/update infrastructure, and clean uninstall semantics.

A conventional signed installer remains a fallback if required Avalonia/Win32 integration or startup behavior proves materially simpler outside MSIX.

Do not choose a packaging mechanism solely because it is easiest for Cursor.

## 3. Distribution channels

Initial product channels should remain minimal:

- `stable` — normal users;
- `internal/dev` — test harness and developer builds.

No beta channel is required until the product has real external testers.

## 4. Code signing

Release binaries/packages must be signed using an appropriate trusted Windows code-signing path. Development builds may be unsigned only in explicitly marked developer environments.

Signing secrets/certificates never belong in repository files or Cursor prompts.

## 5. Update checks

Update infrastructure must be logically separate from the draft data path.

Rules:

- no draft text, fingerprints, field metadata, app usage, URLs, or recovery statistics are included in update requests;
- update failure cannot disable local recovery;
- update code never receives decrypted draft bodies;
- privacy policy changes require explicit release review, not silent configuration migration.

## 6. Update while DraftRescue is running

Updater/package manager may need the process to stop. Before exit:

1. stop new observations;
2. bounded checkpoint of already eligible current state;
3. close repository;
4. exit promptly;
5. update;
6. restart in background if the platform/update mechanism supports it and the user's startup preference allows it.

## 7. Schema migration

Database/config/profile migrations must be versioned and transactional where practical.

Migration rules:

- never decrypt all drafts just to migrate metadata;
- unknown future schema fails safely with a clear repair/update path;
- migration failure leaves old data intact when possible;
- no plaintext migration temp files;
- backups, if ever introduced, must preserve equivalent encryption and retention semantics.

## 8. Downgrade

Automatic downgrade is not supported in MVP. Older binaries encountering newer schema/protection versions must not delete or reinterpret them silently.

## 9. Uninstall

Default uninstall behavior must be designed explicitly before release. Recommended MVP direction:

- uninstall removes application binaries and startup registration;
- user is offered/clearly informed about removal of local DraftRescue data;
- no cloud residue exists because MVP has no cloud account/sync.

Exact MSIX/unpackaged data lifecycle requires packaging experiment before final ADR.

## 10. Offline operation

Core capture, persistence, recovery, Preview, Copy, Restore, retention, and deletion must function without network access. Update availability is optional functionality around the core.

## 11. Acceptance tests

- clean install;
- first launch;
- logon startup enabled/disabled;
- upgrade with active encrypted drafts;
- update failure/offline;
- process restart after update;
- no second observer instance;
- schema migration rollback/failure;
- uninstall data behavior matches documented choice;
- network inspection confirms no draft-bearing update traffic.

## 12. External platform research

Before implementation, verify current Microsoft packaging/MSIX/App Installer guidance and code-signing requirements because deployment guidance changes more often than core Win32 behavior.
