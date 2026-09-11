# DraftRescue — UI/UX Master Specification

Status: **Design baseline for future implementation**  
Scope: MVP-first, privacy-first, intentionally simple  
Audience: product owner, ChatGPT technical direction, future Cursor/coding agents

---

## 1. Product UI principle

DraftRescue is not an app the user should need to manage every day.

The UI should feel like a quiet recovery utility:

- almost invisible during normal work;
- no popups while the user is typing;
- no timeline of everything the user typed;
- no "activity feed";
- no gamification;
- no dashboards or charts;
- no technical telemetry shown to ordinary users;
- clear recovery actions only when a recoverable draft exists.

The core emotional goal is not "look how much DraftRescue captured". It is:

> "A draft was lost. DraftRescue found it. I can get it back safely."

The primary UI is therefore a **small recovery inbox**, not a history application.

---

## 2. Visual direction

### 2.1 Overall style

Use a restrained Windows-desktop aesthetic:

- clean;
- neutral;
- low visual noise;
- rounded corners used sparingly;
- system-like typography;
- strong spacing rather than decorative separators;
- one accent color at most;
- support light and dark system themes;
- avoid gradients, glass effects, oversized illustration, marketing cards, and animation-heavy UI.

The UI should look trustworthy and boring in the positive sense.

### 2.2 Typography

Use the platform/default Avalonia font stack unless a strong reason appears later.

Recommended hierarchy:

- Window/page title: 24–28 px, semibold
- Section title: 16–18 px, semibold
- Draft source/title: 15–16 px, semibold
- Body/preview: 14 px
- Metadata/helper text: 12–13 px

Do not use more than three practical text sizes on a single screen unless necessary.

### 2.3 Spacing

Use an 8 px spacing grid.

Recommended values:

- page outer padding: 24 px
- card padding: 16 px
- major section gap: 24 px
- normal control gap: 8–12 px
- button height: approximately 36–40 px

### 2.4 Color semantics

Do not encode important state by color alone.

Semantic roles:

- default surface/background;
- subtle secondary surface for draft cards;
- system accent for the primary action;
- warning only when there is an actual recoverability/safety limitation;
- destructive styling only for Discard/Delete actions.

No permanent red/yellow "security dashboard". Privacy should be conveyed by behavior, not alarmist decoration.

---

## 3. Information architecture

MVP has only two primary destinations:

1. **Recovery** — recoverable drafts that still exist inside retention.
2. **Settings** — small set of user-facing controls.

An About/version view may be exposed from a small overflow/menu later. It does not need to be a main navigation destination.

Do not create a History tab.

Do not create an Analytics tab.

Do not create a Logs tab for ordinary users.

Do not create an "Everything captured" view.

---

## 4. App presence

### 4.1 Normal operation

During normal typing DraftRescue should produce no visible UI.

No toast per field.
No toast per save.
No "Draft saved" animation.
No character counter.
No recording indicator.

### 4.2 Main window

Recommended initial desktop window:

- default size: ~760 × 560 px;
- minimum size: ~620 × 440 px;
- resizable;
- not maximized by default;
- remembers last normal size/position only if this is later implemented safely and cheaply.

Main layout:

```text
┌──────────────────────────────────────────────────────────────┐
│ DraftRescue                                      [Settings] │
│ Recoverable drafts                                           │
│                                                              │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ Chrome · example.com                                   │ │
│ │ Comment                                                │ │
│ │                                                        │ │
│ │ "This is the beginning of the draft that was..."      │ │
│ │                                                        │ │
│ │ Updated 2 min ago · Expires in 28 min                 │ │
│ │                         [Preview] [Copy] [Restore]     │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ Notepad                                                │ │
│ │ Unsaved text                                           │ │
│ │ ...                                                    │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

This wireframe describes hierarchy only. It is not pixel-perfect styling.

---

## 5. Recovery screen

Recovery is the default main screen.

### 5.1 Header

Left:

- `DraftRescue`
- subtitle: `Recoverable drafts`

Right:

- Settings button/icon.

Do not show capture counters such as "1,482 fields observed".

### 5.2 Empty state

When there are no recoverable drafts:

```text
┌──────────────────────────────────────────────────────────────┐
│ DraftRescue                                      [Settings] │
│ Recoverable drafts                                           │
│                                                              │
│                                                              │
│                    Nothing to recover                        │
│          Recoverable drafts will appear here only            │
│          when DraftRescue can safely keep one.               │
│                                                              │
│                    Protected locally                         │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

Suggested copy:

- Title: `Nothing to recover`
- Body: `Recoverable drafts will appear here when DraftRescue can safely keep one.`
- Optional small reassurance: `Drafts stay on this PC and expire automatically.`

Avoid wording implying that DraftRescue watches or records everything typed.

### 5.3 Draft card

Each recoverable draft is represented as one card/row.

Card hierarchy:

1. application identity;
2. optional safe context label (site/app/window label when available and appropriate);
3. field-purpose label when known and non-sensitive;
4. short preview, if preview is permitted;
5. updated/expiry metadata;
6. actions.

Example:

```text
Chrome · example.com
Comment

"I think the main issue with the proposal is that..."

Updated 2 min ago · Expires in 28 min

[Preview]  [Copy]  [Restore]
```

Never show secure-field metadata that leaks sensitive context.

Never display internal fingerprints, automation IDs, handles, process IDs, hashes, or confidence scores to ordinary users.

### 5.4 Actions

Canonical actions:

- **Restore** — primary when target matching is safe enough.
- **Preview** — non-destructive inspection.
- **Copy** — explicit clipboard action.
- **Discard** — destructive removal.

Default button hierarchy:

- Restore: primary accent button.
- Preview: secondary button.
- Copy: secondary button.
- Discard: menu/destructive tertiary action rather than a visually dominant button.

If automatic Restore is unsafe or unavailable:

- do not fake confidence;
- disable or hide Restore;
- keep Preview and Copy if those operations are safe;
- explain briefly: `The original field could not be matched safely.`

Do not expose a numeric confidence score.

---

## 6. Preview experience

Preview should be simple and non-editing by default.

Recommended presentation: modal/dialog or detail pane.

```text
┌──────────────────────────────────────────────────────────┐
│ Draft preview                                        [×] │
│ Chrome · example.com                                    │
│ Updated 2 min ago                                       │
│                                                        │
│ ┌────────────────────────────────────────────────────┐ │
│ │ Full recovered draft text...                      │ │
│ │                                                   │ │
│ │                                                   │ │
│ └────────────────────────────────────────────────────┘ │
│                                                        │
│ [Discard]                           [Copy] [Restore]    │
└──────────────────────────────────────────────────────────┘
```

Requirements:

- text is selectable;
- text is read-only;
- no rich-text editor in MVP;
- no AI rewrite tools;
- no spell checking requirement for MVP;
- no auto-copy when opening Preview;
- opening Preview must not modify the destination field;
- closing Preview must not alter the draft.

If previewing itself has a privacy implication later (for example screen sharing), that is outside MVP; do not add complex concealment UX prematurely.

---

## 7. Successful restore state

After a successful Restore:

- show a small inline confirmation, not a large modal;
- recommended message: `Draft restored.`
- provide no celebratory animation;
- do not keep a permanent "restored history" entry.

The restored draft's lifecycle behavior must be defined by application/domain rules, not improvised in the ViewModel.

The UI must not silently discard the only recoverable copy before the restore operation reports success.

---

## 8. Restore unavailable / unsafe state

When DraftRescue has the draft but cannot safely match the original field:

```text
Couldn’t safely match the original field.
You can still preview or copy this draft.

[Preview] [Copy]
```

This is an expected safety state, not necessarily an application error.

Do not offer a "Restore anyway" bypass in MVP.

Safety wins over convenience.

---

## 9. Settings screen

Settings must remain compact.

MVP layout:

```text
┌──────────────────────────────────────────────────────────────┐
│ ← Settings                                                   │
│                                                              │
│ Draft retention                                              │
│ How long recoverable drafts may remain on this PC.           │
│ [ 30 minutes                                      ▼ ]        │
│                                                              │
│ Privacy                                                      │
│ Private browsing                         Not saved by default │
│ Secure and credential fields             Never saved         │
│                                                              │
│ Data                                                         │
│ [Discard all recoverable drafts]                             │
│                                                              │
│                                        Version 0.x.x          │
└──────────────────────────────────────────────────────────────┘
```

### 9.1 Retention

Canonical preset values:

- 5 minutes
- 30 minutes
- 1 hour
- 6 hours
- 24 hours
- Custom

Do not use "Forever" or unlimited retention.

The UI copy must make clear that retention controls recoverable drafts, not a typing history.

### 9.2 Privacy summary

For early versions, display important guarantees as informational rows rather than toggles:

- `Private browsing — Not saved by default`
- `Secure and credential fields — Never saved`
- `Cloud sync — Off / Not used`

Do not let users disable the absolute secure-input bypass.

A future advanced privacy/exclusions page may exist, but should not be built until app-profile requirements justify it.

### 9.3 Discard all drafts

Provide a user-controlled way to remove all active recoverable drafts.

Because the action destroys recoverability, require a lightweight confirmation:

`Discard all recoverable drafts? This cannot be undone.`

Buttons:

- Cancel
- Discard all

Do not use frightening language implying account/data deletion beyond DraftRescue's active local drafts.

---

## 10. Notifications

Notifications should be rare.

### Allowed notification category

A notification may be useful when DraftRescue detects a recoverable draft after the user returns to the relevant context.

Example:

- Title: `Draft available`
- Body: `DraftRescue found recoverable text from Chrome.`
- Action: `Open DraftRescue`

Notification text should not contain draft contents.

### Do not notify for

- every detected field;
- every persistence update;
- every application switch;
- expiration of routine drafts;
- secure-field bypass;
- private-browsing bypass.

Security/privacy bypasses should normally be silent.

---

## 11. Tray / background presence

System tray behavior is **not Phase 0 implementation scope**, but the intended UX can be documented now.

If/when a tray icon is added later, keep the menu minimal:

```text
Open DraftRescue
----------------
Settings
About
Exit
```

Do not add a live list of captured text to the tray menu.

A Pause/Resume control should not be introduced casually because its exact safety semantics need design first. Track it as a product decision, not an assumed feature.

---

## 12. Error presentation

Use plain-language errors.

Bad:

`COMException 0x80040201 while resolving AutomationElement.`

Good:

`DraftRescue couldn’t access this field.`

Technical diagnostics belong in privacy-safe logs and development tooling, never as raw exception dumps to the normal UI.

No error message may include draft text unless that text is already intentionally displayed inside Preview.

---

## 13. Accessibility

MVP should still follow basic accessibility rules:

- keyboard navigation for all actionable controls;
- visible focus indicator;
- correct accessible names for icon-only controls;
- reasonable contrast;
- no state communicated by color alone;
- support Windows scaling / DPI;
- text should reflow without clipping at common scaling levels;
- destructive action must not become the default focused action in confirmations.

Do not add custom-drawn controls if standard Avalonia controls can satisfy the requirement.

---

## 14. Animation

Animation is optional and should be minimal.

Permitted later:

- short page transition;
- subtle card appearance/removal;
- small progress indication during an explicit restore operation.

Not permitted as a design direction:

- animated typing indicators;
- pulsing capture/recording indicator;
- decorative background motion;
- large success celebrations.

Reduced-motion settings should be respected if animation is introduced later.

---

## 15. Responsive behavior inside the desktop window

At narrow supported window widths:

- metadata may wrap;
- action buttons may wrap to a second row;
- preview text should truncate by line count, not break layout;
- application identity should remain visible;
- no horizontal scrolling for the primary recovery list.

At larger widths:

- do not stretch text lines excessively;
- keep card content to a comfortable readable measure.

---

## 16. Draft preview truncation on cards

The list screen should never show the full draft by default.

Recommended:

- 2–4 visual lines;
- ellipsis after truncation;
- full text only in Preview;
- preserve line breaks only if it does not make the card excessively tall.

This reduces visual exposure and keeps the recovery inbox scannable.

---

## 17. Sorting

Default sort for active recoverable drafts:

1. newest/most recently updated first;
2. expired drafts never shown;
3. no complex sorting UI in MVP.

If a draft is associated with the user's current context and matching is sufficiently safe, the application may later prioritize it visually, but this must not silently change security rules.

---

## 18. Loading state

Recovery list loading should be quiet.

If loading is effectively instantaneous, show nothing.

If it is perceptible:

- small progress indicator or skeleton rows;
- do not display fake draft cards containing placeholder sensitive-looking text.

---

## 19. First launch

Avoid a multi-page onboarding wizard for MVP.

A first-launch explanation may be one compact screen/dialog if needed:

```text
DraftRescue protects recoverable drafts locally.

• Secure and credential fields are never saved.
• Private browsing is excluded by default.
• Drafts expire automatically.
• Nothing is sent to a server.

[Continue]
```

No account creation.
No cloud sign-in.
No mandatory tutorial.

Whether first-launch UI is needed at all should be decided after the core recovery UX is tested.

---

## 20. UX states the implementation must model explicitly

The UI must not infer domain state from arbitrary strings. Future ViewModels should receive explicit application-level state.

At minimum plan for:

### Recovery collection

- Loading
- Empty
- HasDrafts
- RecoverableDataUnavailable/Error

### Individual draft

- AvailableRestorePossible
- AvailableManualOnly
- RestoreInProgress
- RestoreSucceeded
- RestoreFailed
- Expired
- Discarding

### Restore capability

- SafeToRestore
- TargetUnavailable
- TargetMismatch
- Unsupported

Do not use the UI to override an unsafe restore decision.

---

## 21. What the UI must never expose

Never expose to ordinary users unless a future dedicated diagnostic mode is explicitly designed:

- raw keystroke events;
- per-character capture timelines;
- passwords or secure input;
- secure field candidates;
- internal UI Automation trees;
- window handles;
- process IDs;
- field fingerprints;
- encryption keys;
- DPAPI blobs;
- database rows;
- internal confidence values;
- debug logs containing captured content;
- drafts after retention expiry.

---

## 22. MVP screen inventory

Required eventually:

1. Recovery — empty state.
2. Recovery — one/multiple draft cards.
3. Preview draft.
4. Restore unavailable/safe manual fallback.
5. Settings.
6. Discard-one confirmation where appropriate.
7. Discard-all confirmation.
8. Small success/error inline feedback.

Optional/later:

- tray menu;
- About dialog;
- first-launch explanation;
- advanced application exclusions/profiles.

Not planned:

- History screen;
- analytics dashboard;
- cloud/account screen;
- AI screen.

---

## 23. Cursor implementation constraints for this design

When Cursor eventually implements UI:

1. Do not redesign the product architecture inside ViewModels.
2. Do not add new product features because a UI library makes them easy.
3. Do not introduce browser/Electron/platform inspection code inside the Desktop project.
4. Do not put persistence calls directly in views/code-behind.
5. Do not place raw draft content in application logs or exceptions.
6. Do not implement a History screen.
7. Do not make secure/private guarantees configurable if the product spec defines them as absolute.
8. Use reusable simple controls only when duplication justifies them; avoid premature design-system work.
9. Prefer standard Avalonia controls and bindings.
10. Keep code-behind limited to view concerns; behavior belongs in ViewModels/Application services.
11. UI must faithfully represent Restore capability supplied by the application layer; it must not recalculate matching safety.
12. Do not implement any UI described here until the relevant product phase is authorized.

---

## 24. Design acceptance criteria

The UI direction is correct when all of the following are true:

- A new user can understand the Recovery screen without documentation.
- Normal operation creates essentially no visual noise.
- The application never looks like a typing-history recorder.
- Restore is visually primary only when it is actually safe/available.
- Preview and Copy provide a conservative fallback.
- Discard is available but not easy to trigger accidentally.
- Settings explain retention and privacy without overwhelming the user.
- The UI contains no cloud/account/AI concepts.
- Draft contents are shown only where recovery requires them.
- The overall product feels like a small system utility, not a content-management application.

---

## 25. Baseline product decision

For DraftRescue MVP, the preferred UI model is:

**Quiet background utility + small Recovery inbox + compact Settings page.**

This is the default design direction until the product owner explicitly changes it.
