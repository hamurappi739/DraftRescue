# Security Test Fixtures Specification

## 1. Purpose

Build controlled synthetic fixtures so privacy behavior can be tested without scraping real user applications/accounts/data.

## 2. Fixture application

A later test harness should expose deterministic fields:

- ordinary single-line text;
- ordinary multi-line text;
- read-only text;
- password/protected field;
- username/email-like credential field;
- PIN/security-code-like field;
- payment/card-like field;
- field whose security metadata call intentionally times out;
- field whose UIA element becomes stale during read;
- two visually similar fields for recovery ambiguity tests;
- field that changes identity/context generation while a read is delayed.

Use synthetic strings such as `DRAFTRESCUE_CANARY_42_NOT_A_SECRET`, never real credentials.

## 3. Browser fixture pages

If browser tests later need HTML fixtures, host them locally/offline. Include:

- ordinary textarea;
- password input;
- username/email input;
- one-time-code style input;
- payment card-like input;
- two same-label textareas;
- page/navigation transitions;
- same origin different path;
- distinct origins using local hostnames if practical.

The fixture itself must not transmit entered values.

## 4. Deliberate race controls

Test harness should expose developer-only controls to:

- delay metadata response;
- delay text read;
- replace control instance while keeping same visual label;
- toggle protected state;
- close window during restore;
- change field contents between match and write.

## 5. Canary scanning

Critical tests write an obvious synthetic canary into allowed fields and then scan:

- normal log files;
- database file and rollback journal if present;
- settings/profile files;
- crash diagnostics produced by controlled exceptions.

The canary may appear only inside transient memory and protected ciphertext, never as recoverable plaintext in durable artifacts.
## 6. Phase 2 capability tests

Use `phase2-security-fixtures.v1.json` for policy decisions and `allowed-field-capability-test-fixtures.v1.json` for lifecycle tests. Production capabilities are never serialized; the latter file describes synthetic test scenarios only. See `PHASE2_SECURITY_HARNESS_ARCHITECTURE.md`.
