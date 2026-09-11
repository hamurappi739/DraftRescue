# Controlled Test Harness Design

**Status:** specification only. Do not add the harness to Phase 0 solution until its work package begins.

## 1. Why a harness exists

DraftRescue's most dangerous failures are privacy/race failures. Testing them only against real Chrome/Notepad/Discord would be nondeterministic and could expose real user content. A local synthetic harness gives deterministic UI Automation surfaces and deliberate failure controls.

## 2. Future project

Suggested tool project:

```text
tools/DraftRescue.TestHarness
```

This is developer/test tooling and must never ship in the production package.

## 3. Main fixture window

Expose clearly labeled synthetic controls:

1. Ordinary single-line field
2. Ordinary multi-line field
3. Read-only field
4. Password/protected field
5. Username/email-like field
6. PIN/OTP-like field
7. Card/payment-like field
8. Two identical-label ordinary fields
9. Large-text field
10. Unicode/emoji/RTL test field

All fixture text is synthetic.

## 4. Race lab

Developer controls trigger deterministic races:

- Replace focused element after N ms
- Close window after N ms
- Toggle protected state after N ms
- Move focus to sibling field after N ms
- Delay accessibility/provider response when technically feasible
- Change automation ID/context generation
- Clear field programmatically
- Simulate strong completion signal

## 5. Recovery lab

Support close/reopen modes that recreate:

- same logical window/field identity;
- same app but different field;
- same labels but different hierarchy;
- changed geometry;
- conflicting application/context token.

Expected match result is shown in developer-only harness UI, never in production UI.

## 6. Persistence canary

Use obvious fixture strings:

```text
DRAFTRESCUE_CANARY_ALLOWED_001
DRAFTRESCUE_CANARY_PASSWORD_MUST_NEVER_READ
DRAFTRESCUE_CANARY_PRIVATE_MUST_NEVER_READ
```

Canary tooling scans product-owned durable artifacts. It never scans unrelated user files.

## 7. Browser fixture server

Later browser work may add a loopback-only static fixture server or file-based local test pages. It must not record submitted text and must not bind externally by default.

## 8. CI separation

Pure unit/architecture tests run everywhere supported by CI. Windows/UIA/harness integration tests are clearly tagged and run on controlled Windows agents/sessions because interactive desktop requirements differ from ordinary headless tests.
