# Installation Secret Lifecycle Specification

## Purpose

DraftRescue needs a random local secret for keyed HMAC fingerprints so stored correlation tokens cannot be trivially dictionary-tested from raw titles/domains/labels.

## Secret

- 256 random bits generated from a cryptographically secure RNG;
- generated once per DraftRescue installation/user data identity;
- never derived from username, SID, machine name, executable path, or product license;
- protected with DPAPI `CurrentUser` before durable storage;
- plaintext key exists only transiently in process memory.

## Storage

Store a small versioned file separate from `drafts.db`:

```text
installation-secret.bin
  format_version
  protected_key_blob
```

No plaintext key in settings JSON, registry, logs, crash metadata, or database.

## First startup

1. resolve per-user local data directory;
2. ensure private application directory exists;
3. if secret file absent, generate random key;
4. protect with DPAPI CurrentUser;
5. atomically create secret file;
6. if concurrent create loses race, discard generated key and load the winner;
7. never overwrite an existing valid identity silently.

## Secret unavailable/corrupt

Fail closed for operations that require fingerprint derivation/matching. Do not generate a new key while old encrypted drafts still exist and pretend they are matchable.

Recovery choices are explicit:

- if no drafts exist: reset identity may be safe;
- if drafts exist: mark store/identity unavailable and offer explicit discard/reset flow later;
- never export plaintext to migrate around the problem automatically.

## Rotation

No automatic periodic rotation in MVP. Rotation would invalidate fingerprints and requires a dedicated migration/security design.

## Uninstall/reinstall

Product behavior must be explicit: deleting app data removes the secret and makes surviving orphaned database records unusable. Installer must not promise recovery across profile/app-data deletion.
