# Privacy & Security Invariants

These are product invariants, not optional implementation preferences.

## Data that must never become recoverable drafts

- password fields;
- PIN fields;
- security-code fields;
- banking/financial credential fields;
- credential/authentication fields;
- secure-input surfaces;
- private/incognito browser contexts by default;
- any context whose safety classification is uncertain.

## Data handling invariants

1. DraftRescue stores only temporary recoverable drafts, never a long-term history of everything typed.
2. Draft contents remain local.
3. Persistent draft payloads must be encrypted once persistence exists.
4. Draft contents never enter normal logs.
5. Retention expiry deletes recoverable data.
6. Security decisions happen before persistence.
7. Restore must target the intended field; ambiguity is a reason to require safer behavior rather than blind injection.
8. Clipboard use, if added later, is a leakage risk and must be explicitly designed and tested.

## Fail-closed policy

`CaptureEligibility` in the Application layer intentionally allows persistence only when the decision is explicitly `Allowed`. Secure, private, unsupported, and uncertain cases all return `MayPersist == false`.

This is an architectural guardrail: future adapters may enrich detection, but uncertainty must never silently turn into permission.

## Security review gates for future phases

Before any phase that can observe or store real user text is accepted, verify:

- what API supplies the text;
- whether the API exposes secure controls;
- how secure/private contexts are detected;
- what happens on detection failure;
- exactly where plaintext exists in memory;
- exactly where encryption begins;
- whether any exception/logging path can serialize plaintext;
- deletion/retention behavior;
- CPU/memory impact;
- recovery-target ambiguity behavior.
