# Master Test Strategy

**Status:** cross-phase quality contract.

## 1. Test pyramid for DraftRescue

### Unit tests

Fast tests for:

- lifecycle policy;
- fail-closed capture eligibility;
- retention calculations;
- matching evidence rules;
- completion assessments;
- app-profile capability policy;
- ViewModel presentation rules.

No Windows desktop required.

### Architecture tests

Verify:

- Domain has no platform/UI/storage dependencies;
- Application does not reference Win32/Avalonia/concrete storage;
- Desktop does not contain security/persistence implementations;
- prohibited packages/APIs do not leak across boundaries.

### Windows integration tests

Use controlled test applications/fixtures for:

- foreground/focus detection;
- UI Automation metadata;
- password-field blocking;
- supported field snapshot reading;
- restore capability.

These tests must not run against arbitrary personal applications/data.

### End-to-end recovery tests

Synthetic known text only. Never use real secrets/credentials.

## 2. Mandatory privacy negative tests

A release cannot pass with only positive recovery demos.

Negative cases are first-class:

- password;
- PIN/passcode/OTP surfaces;
- banking/payment credential surface;
- private/incognito browser;
- unsupported control;
- provider failure;
- ambiguous restore target;
- cross-origin browser target;
- expired draft;
- secure field appearing after a previously safe field.

Expected result is no capture/persistence/restore as appropriate.

## 3. Content leakage tests

Use a unique synthetic canary such as `DR_TEST_SECRET_<random>`.

After exercising flows, assert the canary is absent from:

- normal logs;
- exception text;
- crash/report artifacts produced by DraftRescue;
- unencrypted storage files;
- temp files owned by DraftRescue;
- diagnostic snapshots.

The test can inspect product-owned artifacts only; do not turn tests into system-wide secret scanners.

## 4. Persistence tests

- ciphertext does not equal plaintext;
- storage file does not contain canary plaintext;
- restart recovers valid current draft;
- expiry removes it;
- discard removes it;
- successful restore removes it;
- crash during update preserves either previous valid snapshot or new valid snapshot, never partial garbage presented as text.

## 5. Race tests

- focus changes during classification;
- field changes between allow and text read;
- field changes during recovery matching;
- draft expires during Preview/Restore;
- app closes during restore;
- submit and process close occur nearly simultaneously;
- shutdown during persistence debounce.

## 6. UI tests

Verify state behavior, not pixel trivia:

- empty recovery state;
- list of recoverable drafts;
- Restore hidden/disabled when unsafe;
- Preview does not copy automatically;
- Discard requires intended confirmation behavior;
- expired item disappears;
- errors contain no raw platform exception/user content.

## 7. App profile certification matrix

Each profile ships with a table containing:

- app version/build tested;
- Windows version tested;
- safe field positive cases;
- secure/private negative cases;
- completion behavior;
- recovery/mismatch behavior;
- known limitations.

## 8. Manual adversarial checklist

Before widening app support, manually attempt:

- switching rapidly between password and normal fields;
- opening normal and private browser windows side-by-side;
- changing DPI/monitor/window size;
- restarting target app;
- running target elevated when DraftRescue is not and vice versa;
- provider/tree changes after app update;
- long continuous typing;
- abrupt DraftRescue kill and PC/app restart simulation.

## 9. Definition of done for any text-capable phase

A phase is not complete until:

- positive functionality passes;
- negative privacy tests pass;
- logs/storage leakage checks pass;
- resource measurements are recorded;
- known unsupported cases fail closed;
- changed files and new assumptions are documented.
