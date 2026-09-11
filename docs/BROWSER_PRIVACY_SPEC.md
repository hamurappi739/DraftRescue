# Browser Privacy & Context Specification

**Status:** design contract for Phase 7.

## 1. Scope

Initial browser support priority:

1. Chrome
2. Edge

Private/incognito modes are denied by default.

## 2. Privacy rule

DraftRescue must establish that the browser context is ordinary/non-private **before browser field content is eligible**.

If private-mode status cannot be determined reliably for the current supported browser version/context, browser capture fails closed.

## 3. Browser context minimization

Recovery may need browser context, but URLs can contain sensitive data.

Prefer this hierarchy:

1. normalized browser profile identity;
2. origin (`scheme + host + effective port`) where safely obtainable;
3. coarse route/page family only when an adapter proves it is necessary;
4. never persist full query strings/fragments by default.

Do not store browsing history. Context exists only to support a currently recoverable draft.

## 4. Private-mode detection

Do not rely on one cosmetic window-title string globally.

A browser profile should combine stable signals available for that version/platform and be tested against:

- normal window;
- private/incognito window;
- multiple normal/private windows concurrently;
- profile switching;
- browser startup/restore;
- localization/theme changes where relevant.

Unknown = deny.

## 5. Tabs and navigation

Tab identity is ephemeral. A persisted native window handle/process ID must not be treated as durable page identity.

Navigation alone does not prove successful submission. See `SUBMISSION_AND_CLEAR_DETECTION.md`.

## 6. Browser UI versus page content

Do not capture browser chrome fields such as:

- address bar;
- browser search/omnibox;
- password-manager UI;
- browser settings/account fields.

Browser profiles must positively recognize supported page editor surfaces rather than treating every editable automation element in the browser process as eligible.

## 7. Authentication/payment surfaces

Even in normal browsing mode, login, OTP, password reset, payment, banking, and credential-like forms must be denied.

The secure classifier should use provider metadata/profile context before reading field content where possible.

## 8. Cross-origin safety

A recoverable draft from one origin must not direct-restore into a similar field on another origin.

Same visual layout is irrelevant without origin/profile corroboration.

## 9. Recovery UI labels

UI may show a safe site label such as a hostname if the browser adapter marks it safe for presentation.

Do not show full sensitive URLs by default.

## 10. Acceptance gate for browser release

Chrome/Edge support is not accepted until:

- private mode reliably denies in the tested matrix;
- login/payment negative cases pass;
- normal supported editor recovery passes;
- cross-origin mismatch blocks Restore;
- browser update fallback becomes Unsupported/Uncertain rather than permissive;
- idle CPU and event volume stay inside performance budgets.
