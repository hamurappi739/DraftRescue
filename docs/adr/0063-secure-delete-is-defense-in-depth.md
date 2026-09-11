# ADR 0063 — secure_delete is defense-in-depth, not a wipe guarantee

**Decision:** use SQLite `secure_delete=ON` but never claim forensic physical erasure.

**Reason:** SSD wear leveling, snapshots, pagefile, backups and other storage layers are outside SQLite/application control. Primary protection is never writing plaintext at rest.
