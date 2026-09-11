# Secure / Sensitive Context Classification Specification

**Status:** mandatory privacy contract for Phase 2 and beyond.

## 1. Core rule

DraftRescue uses **positive permission**, not blacklist-only filtering.

A field is eligible only when the platform/profile can establish enough trusted metadata to classify it as an ordinary supported editable field.

```text
Explicitly safe + supported -> Allowed
Anything else              -> Denied / Uncertain
```

## 2. Classification outcome

Conceptual outcome maps to the existing `CaptureEligibility` policy:

- Allowed
- SecureField
- CredentialLikeField
- BankingOrFinancialField
- PrivateBrowsing
- UnsupportedContext
- UncertainContext

Only `Allowed` may cross the content/persistence gate.

## 3. Signal priority

Signals should be combined. A weaker allow signal must never override a stronger deny signal.

### Tier 0 — hard deny

- UI Automation `IsPassword == true` or equivalent protected-content signal;
- platform reports a password/secure control;
- known private/incognito browser context;
- app profile explicitly blocks the field/window;
- DraftRescue cannot reliably identify the editable element;
- security detector errors/timeouts where safe classification cannot be completed.

### Tier 1 — credential/financial deny heuristics

Use metadata-only hints where possible:

- automation/name/label purpose indicates password, PIN, passcode, OTP/security code, card verification code, account credential, banking credential;
- app/site profile identifies authentication/payment surfaces;
- field semantic metadata exposed by the accessibility provider indicates protected/credential purpose.

These heuristics should be conservative and testable. Avoid reading field content to decide whether its content “looks like” a password or card number.

### Tier 2 — ordinary editable allow

Allow only when:

- application/profile is supported by the current phase;
- field is editable and not read-only;
- no deny signal is present;
- required metadata queries succeeded;
- context stability is sufficient to bind the observation to one field.

## 4. IsPassword is necessary but not sufficient

Windows UI Automation exposes an `IsPassword` property for protected content. Treat `true` as an immediate hard deny.

Do **not** treat `false` as a universal guarantee of safety. Accessibility providers can be incomplete, custom controls may behave differently, and credential/financial fields are broader than password controls.

## 5. No content-based security classifier in MVP

Do not inspect captured text to determine whether it resembles:

- a credit-card number;
- a password;
- an OTP;
- a PIN;
- a secret/token.

Such a design requires reading sensitive content before deciding it was sensitive, which violates the preferred gating order.

## 6. Failure behavior

Any of the following must resolve to `Uncertain`/deny:

- UI Automation element disappeared mid-query;
- provider throws or times out;
- required property is unavailable and no app-profile safe fallback exists;
- field identity changes while classification is running;
- browser privacy state cannot be established where browser capture requires it;
- process boundary or integrity-level restrictions prevent trustworthy inspection.

## 7. TOCTOU protection

Security classification can become stale between metadata check and text read.

Before reading content, adapters should verify that the candidate still represents the same app/window/field identity. If the field changed, restart classification rather than carrying forward permission.

## 8. Fresh-read capability rule

An `Allowed` policy decision is not ambient permission and is not reused for multiple content reads. Every future content-read attempt requires fresh metadata security evaluation and a new one-read `AllowedFieldHandle` (ADR 0039).

Platform metadata may use narrowly scoped UI Automation caching inside one evaluation attempt, but the resulting Allow decision is not cached as field/process trust. Never cache “this process is safe” as permission for all fields in that process.

Deny/backoff information may be cached carefully for performance only when it cannot become permission; invalidate it when context/profile/version changes.

## 9. Logging

Allowed logs:

- classification result category;
- detector duration;
- opaque app/profile identifier;
- provider capability flags if they contain no user content.

Forbidden logs:

- field Value/TextPattern content;
- raw labels/window titles/URLs unless specifically proven safe and minimized;
- typed text used in any security heuristic.

## 10. Required Phase 2 tests

- native password box denied;
- ordinary native edit allowed only in explicitly supported test profile;
- provider exception => denied;
- disappearing element => denied;
- unsupported custom control => denied;
- known credential field that is not technically password-protected => denied by metadata/profile;
- browser private mode => denied once browser support exists;
- allow never overrides any active deny signal;
- no text reader is called for denied cases where the platform architecture permits pre-read classification.
