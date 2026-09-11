# Retention and Expiry Execution Specification

## Product rule

Recoverable drafts are temporary. Expiry is metadata-driven and never requires decryption.

## Canonical expiry

```text
ExpiresAtUtc = UpdatedAtUtc + configuredRetention
```

Default retention remains 30 minutes unless product settings later change the canonical default. Custom values are bounded by product settings policy; no `Forever` option.

## Enforcement layers

Expiry is enforced redundantly:

1. **query-time:** listing excludes `expires_at_utc_ms <= now` even if row cleanup has not run;
2. **startup:** delete expired rows before exposing recoverable list;
3. **runtime:** low-frequency cleanup timer;
4. **action-time:** Preview/Copy/Restore re-check expiry before decrypting/acting.

## Clock movement

Wall-clock can move backwards. During one process lifetime, once a record has been classified expired it must not be intentionally resurrected because the clock changed.

## Cleanup transaction

```sql
DELETE FROM drafts
WHERE expires_at_utc_ms <= $now;
```

No decryptor/protector calls.

## Retention setting changes

Changing retention applies to future checkpoints and may shorten existing records according to an explicit application-layer recalculation policy. It must never extend already-expired records back into visibility.

## Deletion failure

Expired rows remain hidden even if physical deletion fails. Storage cleanup retries with bounded backoff and a structural error; plaintext is never involved.
