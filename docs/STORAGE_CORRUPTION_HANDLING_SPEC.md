# Storage Corruption and Repair Specification

**Status:** conservative persistence failure behavior.

## 1. Principle

Corruption must not cause DraftRescue to guess plaintext, weaken encryption, expose other drafts, or silently delete the entire database at first error.

## 2. Failure classes

- database cannot open;
- schema version unsupported;
- row metadata invalid;
- ciphertext/protection envelope invalid;
- DPAPI unprotect failure;
- transaction/IO failure;
- individual record inconsistent/duplicated.

## 3. Startup behavior

Metadata listing validates schema/record shape without bulk decrypting. Invalid records are excluded from actionable UI until handled.

One invalid record should not prevent other independently valid records from being listed/recovered where repository integrity allows it.

## 4. No plaintext salvage

Do not scan arbitrary database bytes or process memory trying to reconstruct plaintext. No "best effort" decode with alternate keys/scopes.

## 5. User-facing repair

MVP safe recovery options:

- retry/open later for transient IO issue;
- discard an unreadable recoverable record if identifiable safely;
- reset all DraftRescue local data after explicit confirmation when storage is unrecoverable.

Do not offer export of corrupt encrypted blobs as a normal user feature.

## 6. Diagnostics

Log structural error code, schema/protection version, operation, and hashed/non-user-derived identifiers as approved. Never log ciphertext bytes, plaintext, raw URL/title/label, or DPAPI exception data if it can carry payload bytes.

## 7. Atomicity expectation

Repository operations should make abrupt termination leave either the old valid current record or the new valid current record, not an intentional revision trail.

## 8. Tests

- truncate DB copy;
- flip ciphertext bytes in one synthetic row;
- unknown envelope version;
- DPAPI unavailable/failure injection;
- disk full/read-only directory;
- duplicate logical record;
- schema newer than binary;
- reset after corruption does not require decryption.
