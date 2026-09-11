# Notepad Phase 1 Experiment Plan

**Status:** reconnaissance only. This document does not certify Notepad as supported.

## Why Notepad first

Notepad is useful as an initial real-world Windows target because the product master context already names it as a test target, while its primary document surface is easier to reason about than a browser form containing credentials/payment inputs.

## Scope

Phase 1 Notepad work observes metadata only.

Allowed:

- process/package/version identity;
- foreground/focus behavior;
- Tier A UIA properties;
- bounded Tier B structural properties under explicit experiment logging rules;
- control pattern availability booleans;
- runtime/topology stability;
- provider latency/failure behavior.

Forbidden:

- document text;
- file contents;
- filename/window-title persistence;
- clipboard;
- Save/Open dialog contents;
- generic fallback keyboard hooks.

## Scenarios

1. launch empty Notepad;
2. focus document surface;
3. focus menu/settings/chrome;
4. open Save As dialog;
5. open Find/Replace if available;
6. switch between two Notepad windows/tabs if current version supports them;
7. close/reopen document surface;
8. resize/maximize/minimize;
9. move between monitors/DPI settings;
10. rapidly Alt-Tab away/back;
11. restart Notepad;
12. update/build-version change when test machines permit;
13. run DraftRescue non-elevated against elevated Notepad and verify fail-closed behavior.

## Evidence to collect

Only content-free structural evidence:

- normalized control kind;
- framework kind;
- password flag state;
- pattern availability flags;
- stable/unstable AutomationId/Class token fingerprint where allowed;
- runtime-id stability within process;
- focus/foreground event sequence;
- timing and failures;
- synthetic topology notes.

## Promotion path

Notepad may move from `experimental` to `supported` only after:

1. Phase 2 classifier can positively identify the document surface and exclude dialogs/other fields before text read;
2. test harness proves zero reads in negative surfaces;
3. version compatibility is bounded;
4. recovery identity evidence is defined;
5. restore behavior is separately certified in Phase 6;
6. release privacy checklist passes.

Phase 1 alone cannot promote support.
