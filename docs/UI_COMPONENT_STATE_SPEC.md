# UI Component State Specification

**Status:** accepted MVP presentation contract.

## 1. Principle

The UI is a thin presentation layer over application use-case results. It never derives capture eligibility, recovery confidence, expiry authority, or restore safety itself.

## 2. Recovery page

States:

```text
LoadingMetadata
Empty
Ready(items)
StoreUnavailable
FatalConfigurationError
```

`LoadingMetadata` must not decrypt bodies.

`Empty` copy:

- `Nothing to recover`
- `Recoverable drafts will appear here when DraftRescue can safely keep one.`

## 3. Draft card

Displayed by default:

- application display name/icon when safely available;
- coarse `DraftPresentationKind` label;
- `Updated ...`;
- `Expires ...`;
- action availability.

**MVP does not automatically show a decrypted text snippet on the card.** Text content appears only after explicit Preview.

Card action state model:

```text
Preview: Enabled | Busy | Unavailable
Copy:    Enabled | Busy | Unavailable
Restore: Enabled | Busy | UnavailableUnsafe | UnavailableNoTarget | UnavailableUnsupported
Discard: Enabled | Busy
```

Only one destructive/mutating action for a card runs at a time.

## 4. Restore button

Enabled only when the application layer reports Restore is currently eligible to attempt. Clicking it still triggers full fresh revalidation; availability is not authorization.

During operation:

- disable Restore/Copy/Discard for that card as appropriate;
- show a small progress indicator/label such as `Restoring…`;
- repeated clicks do not launch another mutation.

Results:

- `VerifiedRestored` -> remove card, inline `Draft restored.`;
- `AppliedButUnverified` -> keep card and explain `Draft was inserted, but DraftRescue couldn't verify the result. The recovery copy was kept.`;
- ambiguous/unsafe/changed -> keep card; offer Preview/Copy;
- write/verification failure -> keep card.

## 5. Preview dialog

States:

```text
Loading
Ready(plaintext)
Expired
Unavailable
ErrorContentFree
```

Opening Preview is an explicit content-reveal action.

Requirements:

- no plaintext before `Ready`;
- selectable read-only text;
- no automatic clipboard mutation;
- closing releases ViewModel references to plaintext as soon as practical;
- closing returns focus to the invoking card/action;
- Escape closes when no destructive confirmation is active.

## 6. Copy

During Copy, only selected draft is decrypted. On success show a short inline/toast confirmation `Copied.`. No automatic clipboard clear timer.

Copy failure does not reveal exception text.

## 7. Discard

Single draft:

- one-step destructive action may be acceptable if placed in overflow/menu and clearly labeled `Discard draft`;
- once confirmed/activated, idempotent command.

Discard all:

- always confirmation dialog;
- copy: `Discard all recoverable drafts?` / `This permanently removes all drafts currently available for recovery.`

## 8. Settings controls

### Retention

- presets: 5 min, 30 min, 1 h, 6 h, 24 h;
- custom: 1 minute–24 hours;
- invalid custom value cannot be saved.

### Start with Windows

Simple toggle with explanatory helper text. Exact registration mechanism is platform/package responsibility.

### Privacy rows

Read-only statements, not toggles that weaken hard safety rules:

- `Private browsing — Not saved`
- `Passwords and security fields — Never saved`
- `Cloud text processing — Not used`

No “capture passwords” advanced switch exists.

## 9. Tray menu

Minimal MVP:

```text
Open DraftRescue
---
Quit DraftRescue…
```

Optional Settings shortcut is acceptable. Do not list/decrypt drafts inside the tray menu.

## 10. Accessibility

- full keyboard navigation;
- logical tab order;
- visible keyboard focus;
- accessible names for icon-only controls;
- state never conveyed by color alone;
- dialogs trap focus correctly and return focus on close;
- screen-reader labels avoid leaking more context than visible UI already shows;
- minimum action target sizing appropriate for desktop pointer use.

## 11. Window close

Close hides/closes main window but does not stop protection. Explicit Quit is separate and warned according to `SYSTEM_LIFECYCLE_UX_SPEC.md`.
