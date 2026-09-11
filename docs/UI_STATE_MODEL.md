# DraftRescue — UI State Model

Status: design contract for future ViewModels/application presentation models.

This document prevents future UI code from inventing security-sensitive behavior.

## 1. Principle

UI state is a projection of application/domain decisions.

The Desktop layer may decide presentation (visibility, layout, wording), but must not decide whether a draft is safe to capture, safe to persist, or safe to restore.

## 2. Recovery screen state

```text
RecoveryScreenState
├── Loading
├── Empty
├── Ready
│   └── DraftItems[]
└── Failed
    └── SafeUserFacingError
```

`Failed` messages must not contain raw draft content, field values, credentials, or raw platform exceptions.

## 3. Draft item presentation state

Conceptual future model:

```text
DraftPresentationItem
- DraftId
- SafeApplicationLabel
- SafeContextLabel?
- SafeFieldLabel?
- PreviewText
- UpdatedAt
- ExpiresAt
- RestoreCapability
- OperationState
```

`PreviewText` is user content and must be handled as sensitive local data even though it is intentionally rendered to the user.

Do not store UI-derived preview text in logs, analytics, crash metadata, or telemetry.

## 4. Restore capability

```text
RestoreCapability
- SafeToRestore
- TargetUnavailable
- TargetMismatch
- Unsupported
```

UI behavior:

| Capability | Restore | Preview | Copy | Explanation |
|---|---|---|---|---|
| SafeToRestore | Enabled | Enabled | Enabled | None required |
| TargetUnavailable | Disabled/hidden | Enabled | Enabled | Original field is unavailable |
| TargetMismatch | Disabled/hidden | Enabled | Enabled | Original field could not be matched safely |
| Unsupported | Disabled/hidden | Enabled | Enabled | Automatic restore is not supported here |

No "force restore" bypass in MVP.

## 5. Operation state

```text
DraftOperationState
- Idle
- Restoring
- RestoreSucceeded
- RestoreFailed
- Discarding
- CopyFeedback
```

UI must prevent accidental duplicate destructive operations while `Discarding`.

Restore button should not be repeatedly invokable while `Restoring`.

## 6. Expiration race

A draft may expire while visible.

Required future behavior:

1. application layer rejects use of an expired draft;
2. UI removes/refreshes the item;
3. show a small message such as `This draft has expired.` if the user had just attempted an action;
4. never restore stale cached UI text after application state says the draft is expired.

## 7. Copy behavior

Copy is always explicit.

The UI must never automatically copy draft text on:

- opening Preview;
- selecting a card;
- restore failure;
- application startup.

If future clipboard-clearing behavior is considered, it requires a separate product/security decision; do not assume it.

## 8. Discard behavior

Single-draft discard may use either:

- a compact confirmation if accidental activation is plausible; or
- an undo pattern only if the underlying data lifecycle can support it without weakening privacy semantics.

For MVP, prefer a straightforward confirmation over building deleted-item retention merely to support undo.

`Discard all` always requires confirmation.

## 9. ViewModel boundary

ViewModels may:

- expose commands;
- map application presentation data to display properties;
- coordinate dialogs/navigation;
- expose loading/error state.

ViewModels must not:

- inspect Win32/UI Automation;
- infer password/security field status;
- open persistence databases directly;
- decrypt storage directly;
- calculate recovery matching confidence;
- bypass restore eligibility;
- log raw draft text.

## 10. Code-behind boundary

Code-behind should be limited to strictly visual/window concerns that are awkward to express through binding.

No business logic, persistence, capture logic, matching logic, or privacy classification in code-behind.
