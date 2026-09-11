# Shutdown, Crash, Restart, and Persistence Test Matrix

**Status:** future integration/destructive-test plan.

| ID | Scenario | Expected durable result |
|---|---|---|
| LIFE-001 | Normal explicit Quit after committed snapshot | latest committed eligible snapshot remains until expiry |
| LIFE-002 | Quit during debounce before next checkpoint | previous committed snapshot valid; no corruption |
| LIFE-003 | `WM_QUERYENDSESSION` arrives while idle | prompt response; no shutdown blocking |
| LIFE-004 | session end while UIA call is hung | shutdown not held indefinitely; last commit survives |
| LIFE-005 | terminate process immediately after transaction commit | committed record readable after restart |
| LIFE-006 | terminate process during update transaction | old or new valid state, never malformed plaintext |
| LIFE-007 | power loss simulation after dirty in-memory update | only prior committed state guaranteed |
| LIFE-008 | restart with expired records | expired items invisible immediately, cleanup follows |
| LIFE-009 | one corrupt protected record | that record unavailable; other records remain usable |
| LIFE-010 | unknown crypto envelope version | fail closed; no fallback/decode guessing |
| LIFE-011 | database schema newer than binary | no destructive downgrade/migration |
| LIFE-012 | double process start after login | exactly one observer/repository writer |
| LIFE-013 | updater closes process | bounded checkpoint + clean repository close where possible |
| LIFE-014 | Windows force-terminates process | subsequent startup remains consistent |

## Destructive test note

Use synthetic canary drafts only. Never run destructive lifecycle tests against a real user's production DraftRescue data directory.
