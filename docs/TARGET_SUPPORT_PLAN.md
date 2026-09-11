# Target Support Plan

**Purpose:** prevent broad "works everywhere" development. Support is earned one target family at a time.

## Stage A — Synthetic fixture

Before relying on third-party apps, build a controlled Windows fixture application for security/race tests.

Goals:

- ordinary editable field;
- password field;
- read-only field;
- synthetic credential/payment fields;
- multi-line field;
- delayed/stale target controls.

This is the safest place to prove Phase 1–3 plumbing.

## Stage B — Notepad test target

Notepad is a compatibility/recovery test target, not a promise that every Windows editor works.

Certification goals:

- reliable foreground/focused-field metadata;
- secure gating (where relevant fixture support exists separately);
- current snapshot tracking;
- close/reopen recovery;
- one validated restore path or explicit Copy-only limitation.

The exact Notepad accessibility framework varies by Windows/app version and must be recorded in the certification template rather than assumed.

## Stage C — generic ordinary Windows controls

After the fixture and Notepad path are stable, validate a small generic control-family matrix (e.g. simple Value-pattern and Text-pattern editors). A generic profile only supports families proven by fixtures; it is not a wildcard for every editable UIA element.

## Stage D — Chrome

Chrome becomes the first browser target only after private/incognito classification is experimentally proven fail-closed.

Order:

1. map normal vs private accessibility behavior with synthetic local pages;
2. hard-deny private mode;
3. deny browser chrome/login/payment negative fixtures;
4. enable one ordinary textarea/editor scenario;
5. recovery matching;
6. restore or Copy-only certification.

## Stage E — Edge

Repeat browser certification independently. Shared Chromium architecture does not permit assuming Chrome results automatically apply to Edge.

## Stage F — Discord

Electron support comes after browsers. Validate login/credential surfaces separately from message composer. App update drift must fail closed.

## Stage G — Telegram Desktop

Later target, not bundled into Discord work.

## Unsupported behavior

For all stages:

- unknown app/version/profile state may be shown as unsupported internally;
- DraftRescue does not attempt aggressive fallback capture;
- unsupported target must not cause high-frequency polling;
- no promise of universal desktop support in MVP marketing.
