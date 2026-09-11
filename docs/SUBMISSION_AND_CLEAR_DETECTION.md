# Submission, Save, and Clear Detection

**Status:** behavior specification. App-specific implementation is deferred.

## 1. Problem

DraftRescue must distinguish between:

- **unexpected loss** — keep the latest eligible draft recoverable;
- **intentional completion** — stop recovering obsolete text.

There is no universal Windows signal meaning “this message was sent.” The design therefore uses a ranked evidence model and app profiles rather than pretending a generic heuristic is always correct.

## 2. Evidence classes

### A. Explicit app-profile completion signal — strongest

Examples:

- a known Send/Save action is invoked and the tracked field clears;
- an app-specific accessibility event sequence reliably indicates submission;
- a known document-save flow has confirmed persistence semantics.

These signals require tests per profile.

### B. Field clear after a likely completion action — strong when profiled

A transition from substantial non-empty text to empty can mean:

- message sent;
- form submitted;
- user manually selected all and deleted;
- page reset/navigation;
- application replaced the editor.

Therefore **empty alone is not enough**. It becomes high-confidence only when correlated with a trusted completion signal or profile rule.

### C. Stable replacement content — medium

Some apps replace the compose control after send. A new field identity plus disappearance of the old draft can support completion when the profile knows this behavior.

### D. Focus change — weak

Focus changes continuously during normal composition. Never delete a draft solely because focus moved elsewhere.

### E. Window/tab close — not completion

Closing the target is precisely a recovery scenario unless completion was established first.

## 3. MVP policy

For the first supported target:

1. keep the latest active snapshot while the field remains eligible;
2. establish one or more explicit tested completion signals for that target;
3. if completion cannot be established, prefer retaining until retention expiry rather than inventing a send detector;
4. do not create recovery notifications merely because focus changed.

## 4. Manual text deletion

When the user intentionally empties a field without submitting:

- if empty state remains stable for a short debounce window, the active draft should be removed because the current unsaved draft is now empty;
- this must not create a recoverable copy of the deleted previous text by default.

This behavior is important for privacy: DraftRescue should not resurrect text the user deliberately erased from a still-live field.

The exact debounce must be measured; do not hard-code an arbitrary long period into product semantics.

## 5. Text replacement

If a user replaces the whole draft with different text in the same logical field, DraftTracker should update the current snapshot. It must not retain both versions as history.

## 6. Navigation

Browser navigation can represent submit, cancel, accidental navigation, or unrelated page changes.

Generic policy:

- navigation alone does not prove submission;
- a profile may combine navigation with known form behavior;
- if the context disappears without trusted completion evidence, keep a recoverable draft subject to retention.

## 7. Save semantics for editors

For editors with an explicit save concept, such as future Notepad/document-like profiles, “saved” and “draft recoverable” semantics may differ from message composition.

Do not globally assume `Ctrl+S` means safe deletion. App profile must confirm that the currently tracked content was actually saved to durable app storage.

## 8. Evidence model

Conceptual result:

```text
CompletionAssessment
- Unknown
- NotCompleted
- CompletedHighConfidence
```

`Unknown` must not delete content immediately.

Do not expose a raw numerical score to UI.

## 9. Tests required per profile

Every app profile that implements completion detection must test at least:

- type -> focus elsewhere -> return: draft survives;
- type -> manually clear: previous text is not resurrected;
- type -> successful send/save: obsolete draft is removed;
- type -> close/crash without send: draft remains recoverable;
- type -> navigation without proven submit: behavior matches profile policy;
- rapid send/close race;
- clear/retype race;
- submit action that fails at application level if detectable.

## 10. Non-goal

The MVP does not need semantic AI to decide whether a message “looks finished.” Completion is based on observable application behavior, not content meaning.
