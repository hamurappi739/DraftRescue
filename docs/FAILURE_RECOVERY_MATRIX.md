# Failure and Recovery Matrix

**Rule:** failures tend toward loss of DraftRescue functionality, not expansion of data access.

| Failure | Immediate behavior | User impact | Data rule |
|---|---|---|---|
| Foreground API returns null | wait for next event/snapshot | none | no read |
| UIA target stale | reacquire metadata | possible missed checkpoint | no stale read |
| Security metadata timeout | deny uncertain | draft may not be captured | no read |
| Private-mode detector unknown | deny uncertain | browser draft not captured | no read |
| Text reader timeout | abandon snapshot, bounded retry later | latest chars may not checkpoint | no persistence fallback |
| Text reader returns inconsistent target generation | discard snapshot | none | never attach text to wrong context |
| DPAPI protect fails | do not write record | recovery unavailable for that update | never plaintext store |
| Repository transaction fails | preserve prior committed snapshot | latest changes may be absent | no side file with plaintext |
| Database corrupt | fail closed/recovery UI unavailable | existing drafts inaccessible | no automatic upload/dump |
| Fingerprint key cannot unprotect | stop matching/capture requiring it | drafts unavailable | do not generate replacement silently |
| App profile invalid | profile disabled | app unsupported | no permissive fallback |
| Restore target disappears | abort | user can retry/copy | no keystroke spill |
| Restore security state changes | abort | Restore unavailable | no write |
| Recovery match ambiguous | Preview/Copy only | no direct restore | no force button |
| Clipboard set fails | report copy failure | draft remains | plaintext not written elsewhere |
| Retention cleanup fails | retry bounded; hide already-expired records from UI | disk cleanup delayed | expired record never treated recoverable |
| Clock moves backward | use stored expiry + monotonic scheduling where useful | no revival | expired remains expired |
| App shutdown during dirty draft | best-effort bounded checkpoint if eligible | latest chars may be lost | never block indefinitely |
| Process crash during DB update | rely on atomic transaction | old/new complete snapshot | no partial plaintext record |
| Logging sink throws | application continues/degrades | diagnostics reduced | never attach draft text to error |

## Escalation/backoff

Repeated platform-provider failures should create a temporary per-app/profile backoff state to prevent CPU loops. Backoff metadata contains only structural error category, timestamps, and app/profile identity.

## User-facing errors

Keep messages calm and non-technical. Do not show captured content inside exception dialogs. Detailed diagnostics are structural and local.
