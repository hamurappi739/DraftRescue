# Storage Corruption and Quarantine Specification

## Objective

Corrupt local state must fail closed without plaintext salvage, silent partial recovery, or unsafe database uploads.

## Detection classes

- SQLite cannot open database;
- integrity/format error;
- unsupported `user_version`;
- malformed row values;
- unknown protected-record version;
- DPAPI blob cannot be unprotected;
- protected payload decodes but envelope is invalid;
- installation secret is unavailable/corrupt.

## Store-level corruption

If SQLite reports store corruption:

1. stop normal writes;
2. do not repeatedly reopen in a tight loop;
3. move/rename the database to a local quarantine path only if the operation itself is safe and atomic enough;
4. never upload quarantine automatically;
5. create a fresh empty store only after preserving the failure state and only if product policy allows;
6. UI later reports drafts unavailable, not recovered.

## Record-level invalidity

A malformed/incompatible row may be marked unavailable and excluded. Do not reinterpret bytes heuristically.

## Quarantine privacy

Quarantine may contain ciphertext and fingerprint metadata. It is still private application data and must not be copied into logs/support bundles.

## No salvage mode

MVP deliberately has no `strings`, raw-page scan, DB dump, or "recover whatever text we can" feature. Such tooling risks turning encrypted/private storage into an extraction surface.

## User action

A future UI may offer `Discard unavailable recovery data`; it must be explicit and content-free.
