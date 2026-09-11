# Phase 4 Threat Model Delta — Persistence

## New assets

- encrypted draft body;
- match/fingerprint metadata;
- installation HMAC secret;
- SQLite database/journal;
- quarantined corrupt store.

## Threats introduced by persistence

### T4-01 Plaintext durable leak
Serializer/log/temp/DB accidentally writes user text before protection.

**Controls:** protector-before-repository API; canary certification; source guards.

### T4-02 Revision history through database behavior
Every change inserts a row or WAL/pages intentionally preserve a product-visible history.

**Controls:** one row per DraftId; no revision schema; DELETE journal baseline; monotonic update.

### T4-03 Weak/unavailable protection fallback
DPAPI fails and code writes plaintext to "avoid data loss".

**Control:** fail closed; repository call count zero on Protect failure.

### T4-04 Fingerprint dictionary attack
Unkeyed hash of domain/title/label permits offline guessing.

**Control:** installation-local random HMAC key protected by DPAPI.

### T4-05 Corruption salvage leakage
Support/recovery code scans raw DB to extract strings.

**Control:** no salvage mode; local quarantine only.

### T4-06 Stale async overwrite
Old encrypted snapshot arrives after newer one.

**Control:** transactional persisted `SnapshotSequence` comparison.

### T4-07 Retention bypass
Expired row remains visible because cleanup failed.

**Control:** query/action-time expiry enforcement independent of physical deletion.

### T4-08 Overstated deletion promise
Product claims secure wipe it cannot guarantee on SSD/snapshots.

**Control:** explicit secure-delete limitations and conservative product copy.
