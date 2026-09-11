# Completion Heuristics by Target Class

**Status:** canonical behavioral baseline for future app-profile implementation.

## 1. Core rule

There is no universal Windows event meaning "the user's draft is safely submitted or saved". Completion must be established by target-specific evidence. If completion remains uncertain, DraftRescue retains the current recoverable snapshot until retention expiry rather than deleting it early.

Focus loss, window deactivation, tab change, process switch, Enter key activity, navigation, or a field becoming empty are never sufficient by themselves.

## 2. Evidence vocabulary

- **Action evidence** — a known Send/Save/Submit action attributable to the target app.
- **Field-transition evidence** — the tracked editor clears, is replaced, becomes read-only, or changes identity in a known way.
- **Destination evidence** — the app exposes a new message/item/document state consistent with successful completion.
- **Durability evidence** — a document-like target confirms persistence to its own durable storage.
- **Failure evidence** — validation error, offline/send failure, permission failure, unsaved marker remains, or equivalent.

A profile defines combinations, not a single generic score.

## 3. Chat / message composer

Examples: future Discord or Telegram profile.

High-confidence completion normally requires:

1. known submit action or app-specific completion event; and
2. tracked compose field transitions to expected post-send state; and
3. no detected failure state.

Do not treat Enter as send globally. Multiline editors may insert a newline; apps may use Ctrl+Enter, configurable shortcuts, buttons, or IME composition.

Manual clear without submit means the previous text is intentionally gone and should not remain recoverable once the stable-empty policy is satisfied.

## 4. Browser comment / form text area

For generic browser pages, DraftRescue must be more conservative because page semantics are uncontrolled.

Allowed conclusions:

- navigation alone: `Unknown`;
- DOM/control replacement alone: `Unknown` unless profile/site adapter proves semantics;
- field clear after known submit action: candidate high-confidence only when the browser/app adapter validates the transition;
- server-side success page/message: useful only through an explicitly designed site/app adapter, not semantic scraping of arbitrary page text in MVP.

Generic browser support should prefer retention over aggressive completion deletion.

## 5. Native form field

For a conventional Windows form with a known profile:

- explicit known Apply/OK/Submit action plus expected field/window transition can establish completion;
- window close after unknown state does not establish completion;
- modal dismissal via Cancel is intentional abandonment only if the profile can distinguish it reliably; otherwise retain until expiry.

## 6. Document editor / Notepad-like target

Document-like apps have a different notion of completion: the text can remain visible after Save.

A profile may mark the draft non-recoverable only when it can establish that the exact tracked content has become durable in the target application's own storage.

`Ctrl+S` alone is not proof. Save may fail, trigger Save As, be canceled, or save a different state.

For early Notepad testing, recovery behavior may deliberately remain conservative until durable-save semantics are experimentally proven.

## 7. Rich text / contenteditable / custom editors

Treat custom editors as app-specific until their behavior is certified. Do not infer completion from generic UIA events when the control virtualizes/replaces its accessibility subtree frequently.

## 8. Failure precedence

Known failure evidence overrides completion candidates. Example:

```text
Submit action observed
+ field temporarily cleared
+ app reports send failure / draft restored
=> NotCompleted / continue tracking
```

## 9. Race policy

Completion assessment carries the same `ContextGeneration` and logical draft identity as the snapshot it affects. A delayed completion result from an older generation cannot delete a newer draft.

## 10. Profile certification tests

Every supported profile must demonstrate:

- submit succeeds;
- submit fails;
- submit then immediate app close;
- focus loss without submit;
- manual clear;
- clear then immediate retype;
- editor/control replacement;
- multiline Enter behavior;
- IME composition where applicable;
- save canceled / Save As path for document-like targets.

## 11. Explicit non-goals

No content-based AI, NLP, punctuation heuristic, or "message looks finished" classifier is permitted for MVP completion decisions.
