# First Run UX Specification

**Status:** accepted simple onboarding baseline; visual details remain under `UI_UX_MASTER_SPEC.md`.

## Goal

Explain what DraftRescue does and the privacy boundary once, without turning setup into a wizard.

## First explicit launch

Show one compact page/window:

```text
DraftRescue

Recover unsaved text when a supported app or window closes unexpectedly.

✓ Drafts stay on this PC
✓ Secure and credential fields are never saved
✓ Private browsing is not saved
✓ Drafts expire automatically

Start DraftRescue when I sign in     [On]

[Continue]
```

No account creation. No email. No cloud sign-in. No permission theater.

## Continue behavior

Pressing Continue:

- saves the visible startup preference;
- begins/continues background operation;
- opens the normal Recovery empty state;
- does not trigger a fake demo draft unless a future optional tutorial is explicitly designed.

## Startup-launched process

If the app is launched automatically at Windows sign-in after onboarding is complete, do not reopen onboarding or main window.

## Privacy copy rule

Do not say "DraftRescue can never see passwords" if the technical implementation queries metadata around fields. Prefer precise product copy: secure/credential fields are excluded and draft text is not saved/read after hard deny according to the implemented gating guarantees.

## Failure during initialization

If required local storage/cryptographic initialization fails, do not pretend protection is active. Show a small status when the user opens DraftRescue:

```text
Draft protection isn't available right now.
Your text isn't being saved by DraftRescue.
[Try again]
```

No raw technical exception content in the primary UI.
