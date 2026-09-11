# Error Handling Policy

## Privacy failures

When secure/private classification fails or returns incomplete information, treat the context as non-persistable.

## Observation failures

Failure to inspect one application/field must not cause a fallback to broader global capture. Mark the context unsupported and continue safely.

## Persistence failures

Do not silently fall back from encrypted storage to plaintext storage. If protection/persistence fails, keep the product safe even if that means losing recovery capability for that draft.

## Recovery failures

Do not restore into an uncertain target. Preserve the recoverable draft until the user discards it or retention expires, and provide safer user-controlled options where appropriate.

## Logging failures

Do not embed raw user text in error messages to improve diagnostics.
