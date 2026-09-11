# Data Erasure and Reset Specification

## 1. User actions

MVP supports:

- Discard one draft;
- Discard all recoverable drafts;
- automatic expiry cleanup.

A later "Reset DraftRescue" maintenance action may also remove protected fingerprint key and settings after discarding drafts.

## 2. Discard one

Order:

1. identify record by opaque DraftId;
2. remove from recoverable state transactionally;
3. delete row;
4. ensure UI model drops any plaintext preview reference;
5. best-effort clear temporary plaintext buffers;
6. log only structural success/failure + DraftId if acceptable under diagnostics policy.

## 3. Discard all

Must require explicit user action. It deletes all recoverable rows without decrypting them.

The operation should work even when DPAPI unprotect is broken because deletion does not require plaintext.

## 4. Expiry

Expiry has two layers:

- logical: once `now >= ExpiresAt`, record is not recoverable;
- physical: cleanup removes the row as soon as practical.

This prevents a cleanup scheduling failure from extending product retention semantics.

## 5. Fingerprint key reset

The fingerprint key cannot be replaced while keeping existing drafts as normally recoverable because their stored HMAC tokens would no longer correlate.

Reset flow:

1. stop observation;
2. delete all recoverable drafts;
3. delete protected fingerprint key;
4. create a new key on next initialization;
5. resume.

## 6. Uninstall

Installer/uninstaller behavior is a later packaging decision. Product should eventually offer a clear choice to remove DraftRescue local data. Do not assume OS uninstall automatically guarantees physical erasure.

## 7. No anti-forensic promise

DraftRescue minimizes and deletes application-level data but does not claim certified secure erasure from SSD wear-leveling, filesystem snapshots, backups, or a compromised OS.
