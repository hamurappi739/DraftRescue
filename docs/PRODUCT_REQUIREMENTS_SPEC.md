# DraftRescue Product Requirements Specification

## 1. Product statement

DraftRescue is a privacy-first Windows utility that preserves a short-lived local recovery copy of unsaved text from explicitly supported ordinary text fields so the user can recover from accidental tab/window/app/PC loss.

Positioning: **Never lose typed text again.**

## 2. Primary user story

As a user writing a substantial message/comment/form, if the target unexpectedly disappears before I intentionally submit/save it, I want DraftRescue to offer the latest safe local draft so I can recover my work.

## 3. Trust requirement

The user benefit is invalid if the implementation behaves like a keylogger. Privacy constraints are therefore acceptance requirements, not secondary non-functional preferences.

## 4. Functional requirements

### FR-01 Background operation

Application can run quietly without requiring the main window open.

### FR-02 Supported context detection

Detect active supported application/window/editor context through targeted Windows accessibility/platform mechanisms.

### FR-03 Security gate

Before ordinary content is eligible, classify secure/private/unsupported/uncertain contexts and deny them.

### FR-04 Current draft tracking

Maintain the latest current state for a supported eligible logical draft, not historical revisions.

### FR-05 Completion/loss handling

Remove obsolete draft on strong intentional completion/clear evidence; preserve latest eligible state on unexpected target loss.

### FR-06 Local protected persistence

Recoverable payload survives app/window/PC restart in encrypted/protected local storage.

### FR-07 Retention

Expire/delete automatically according to bounded user-configured retention.

### FR-08 Recovery UI

Show active recoverable drafts with simple safe metadata and Preview/Copy/Restore/Discard capabilities according to availability.

### FR-09 Restore safety

Direct Restore requires explicit user action, current secure classification, and strong target match.

### FR-10 App profiles

App-specific behavior is isolated behind profiles/adapters and cannot weaken global privacy policy.

## 5. Privacy/security requirements

### PR-01 No general typing history
### PR-02 No password/PIN/security/banking/credential capture
### PR-03 Private browsing excluded by default
### PR-04 No cloud text processing or cloud AI
### PR-05 No draft content in logs/telemetry/support artifacts
### PR-06 Uncertain safety => deny
### PR-07 No blind/force restore
### PR-08 Copy is explicit; no automatic clipboard fallback
### PR-09 Retention is bounded and automatically enforced
### PR-10 No hidden deleted-draft recycle/history store

## 6. UX requirements

- invisible/quiet during normal work;
- no popup on each character/change;
- recovery screen is simple and task-focused;
- no “activity/history” dashboard;
- destructive actions are clear;
- unsupported automatic restore degrades to Preview/Copy rather than pretending certainty.

## 7. Initial app priority

- Notepad/control harness for validation;
- ordinary Windows text controls;
- Chrome;
- Edge;
- Discord;
- Telegram Desktop later.

## 8. Non-functional requirements

- Windows x64;
- C# / .NET 8;
- Avalonia / MVVM;
- Win32/UI Automation isolated in platform layer;
- event-driven/bounded resource use;
- no unbounded event/text queues;
- crash-consistent current-state persistence;
- testable core without launching UI.

## 9. Success criterion

A real accidental-loss event ends with the user recovering the intended draft safely and feeling that DraftRescue saved meaningful work without compromising trust.
