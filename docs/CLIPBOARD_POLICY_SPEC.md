# Clipboard Policy Specification

**Status:** accepted MVP behavior.

## 1. Clipboard use is explicit only

DraftRescue writes draft plaintext to the Windows clipboard only when the user presses **Copy** for a specific recoverable draft.

No capture, tracking, preview, restore, startup, or diagnostics path reads the user's clipboard.

## 2. No automatic clipboard clearing in MVP

DraftRescue does not automatically clear the clipboard after N seconds.

Reasons:

- another application/user action may replace the clipboard before the timer fires;
- clearing could destroy unrelated clipboard data;
- ownership semantics are race-prone;
- silent clipboard mutation is surprising.

The UI may show a brief `Copied` state without claiming the clipboard is private.

## 3. Clipboard content is a leakage boundary

Once the user explicitly copies a draft, the text is subject to normal Windows clipboard behavior and any clipboard-history/sync features the user has enabled outside DraftRescue.

Product privacy copy should distinguish DraftRescue's own storage from OS clipboard behavior.

## 4. Logging/diagnostics

Never log:

- copied text;
- clipboard contents before/after Copy;
- clipboard history;
- preview string in copy exceptions.

## 5. Failure

If clipboard write fails:

- show `Couldn't copy the draft.`;
- keep the recoverable draft;
- do not use a file/temp-text fallback;
- do not retry indefinitely.

## 6. Future changes

Any automatic clear/clipboard ownership feature requires a dedicated UX/security ADR. It must prove it will not erase newer unrelated clipboard content.
