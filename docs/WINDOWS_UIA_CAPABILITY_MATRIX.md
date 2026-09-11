# Windows UI Automation Capability Matrix

**Status:** research baseline for Phase 1/6 experiments. This file separates known API semantics from app-specific assumptions that must be measured.

## 1. Foreground/focus observation baseline

Preferred direction:

- `GetForegroundWindow` for snapshotting current foreground top-level window;
- narrow `SetWinEventHook` subscriptions for relevant foreground/focus/object events;
- `WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS` as the first configuration to validate;
- dedicated message-loop/thread lifetime owned by the Windows platform layer;
- no global keyboard hook.

Implementation must handle transient null/invalid windows and event races.

## 2. Security metadata

UI Automation exposes an `IsPassword` property indicating protected content. `true` is an immediate hard deny. `false` is not sufficient to prove safety; additional credential/banking/private-browser classification still applies.

## 3. Read capability families

| Capability | Typical purpose | DraftRescue use |
|---|---|---|
| Value pattern | Read/set a simple intrinsic value | Candidate for single-line ordinary edit controls after eligibility |
| Text pattern | Read text/document ranges | Candidate for multi-line/document-like surfaces after eligibility |
| TextEdit pattern | Text-edit specific events/semantics | Research only; do not assume cross-app support |
| Legacy accessibility value | Compatibility fallback in some surfaces | App/profile-specific research only |

The reader chooses capability after security gating. Lack of one pattern does not justify reading through an unsafe broad fallback.

## 4. Write/restore capability families

TextPattern is primarily a text retrieval/selection model and is not a universal write API. Direct Restore therefore requires per-control capability discovery and profile/adapter validation.

Initial preference order to validate:

1. settable Value pattern where supported;
2. validated app-specific adapter;
3. validated legacy accessibility value where explicitly approved;
4. otherwise Copy-only.

Generic keystroke injection is not an automatic fallback.

## 5. Framework matrix to measure

| Target family | Locate focused field | Security signal | Read | Write | MVP priority |
|---|---|---|---|---|---|
| Native Win32 edit | experiment | experiment | Value/Text | experiment | High |
| Notepad current Windows | experiment | experiment | experiment | experiment | High test target |
| Chromium browser HTML input/textarea | experiment | experiment + browser privacy | experiment | experiment | High |
| Edge Chromium | experiment | experiment + browser privacy | experiment | experiment | High |
| Electron (Discord) | experiment | experiment | experiment | experiment | Later |
| Telegram Desktop | experiment | experiment | experiment | experiment | Later |
| WPF/WinUI rich editors | experiment | experiment | Text likely, write varies | experiment | Not initial generic assumption |

"experiment" is intentional. Cursor must not replace it with guessed support.

## 6. Timeout/error rules

- every cross-process UIA operation has a bounded timeout/cancellation strategy;
- timeout while reading security metadata => deny/uncertain, not allow;
- stale element => reacquire metadata and reclassify;
- access denied/provider failure => unsupported/uncertain;
- repeated provider hangs cause temporary app/profile backoff to protect CPU/responsiveness;
- no retry loop may retain plaintext indefinitely.

## 7. Event coalescing

WinEvent/UIA notifications may be noisy. Platform layer may coalesce duplicate metadata events by context generation and short time window, but coalescing must never suppress a security-context change.

## 8. Experiment outputs required

Every target experiment records:

- OS build;
- app version;
- process/framework identity;
- control type/class/AutomationId behavior (redacted/hashed if user-derived);
- IsPassword behavior;
- supported patterns;
- focus event behavior;
- read semantics;
- write semantics;
- private-mode behavior for browsers;
- CPU/event rate;
- failure cases;
- final support classification.
