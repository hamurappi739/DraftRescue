# Local Data Directory Layout Specification

**Status:** logical layout; exact Windows paths are packaging-dependent.

## 1. Principle

Keep durable state small, predictable, user-local, and separable by purpose. Never create caches containing plaintext drafts.

## 2. Logical layout

```text
DraftRescueData/
  db/
    drafts.db
  config/
    settings.json        # if packaging/config model uses file-based settings
  diagnostics/
    app.log              # structural only, bounded rotation
  state/
    installation-key.protected / equivalent protected key material
    instance-state / migration markers as needed
```

Exact file placement may differ under MSIX/package identity. The logical separation remains.

## 3. Forbidden artifacts

- plaintext draft temp files;
- preview cache;
- clipboard cache;
- revision backup files containing old ciphertext indefinitely;
- raw window-title/URL dumps;
- UIA tree dumps from real users;
- crash support bundles containing payloads.

## 4. Permissions

Use normal per-user application-data permissions. Do not weaken ACLs for interoperability. No machine-wide shared draft database.

## 5. Temporary files

If a library/packaging operation creates temp files, test them with plaintext canaries. DraftRescue application code itself should not require plaintext disk temp files for draft operations.

## 6. Cleanup

Reset/uninstall/update behavior must explicitly account for each logical directory. Retention cleanup applies to recoverable draft records, not arbitrary log/config deletion.
