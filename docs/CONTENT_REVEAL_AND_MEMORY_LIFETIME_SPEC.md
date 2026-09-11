# Content Reveal and Plaintext Memory Lifetime

**Status:** accepted privacy/UX contract.

## 1. Principle

Encryption at rest does not eliminate plaintext risk in memory or on screen. DraftRescue therefore reveals content only when the current operation needs it.

## 2. Plaintext entry points

Allowed plaintext paths:

1. eligible snapshot read -> tracker/protector;
2. explicit Preview of one draft;
3. explicit Copy of one draft;
4. authorized Restore of one draft.

Not allowed:

- recovery list preload;
- tray menu;
- logs/metrics/support bundle;
- profile resolver;
- recovery target discovery/matching;
- retention cleanup;
- discard/discard-all;
- startup scan.

## 3. Recovery list

MVP list is metadata-only. No automatic body snippet and no raw dynamic site/window/field label. See `SAFE_PRESENTATION_METADATA_SPEC.md`.

This means the UI may be visually less descriptive, but it avoids decrypting N drafts merely to render a page.

## 4. Preview lifetime

- decrypt selected record only after explicit Preview;
- bind plaintext only to the active preview ViewModel/control;
- do not place body into navigation history or global store;
- on close/navigation, clear references and dispose owned buffers where practical;
- do not claim guaranteed zeroization of immutable .NET strings; prefer narrowly scoped representations and avoid unnecessary copies.

## 5. Restore lifetime

Target matching/security occurs before draft plaintext is requested for mutation. After authorized write/verification, drop plaintext references promptly.

## 6. Clipboard

Clipboard is intentionally outside process memory after explicit Copy and is controlled by Windows/user applications. DraftRescue does not later clear it automatically in MVP.

## 7. Crash diagnostics

Crash tooling/configuration must not capture arbitrary managed object graphs containing preview/restore plaintext for upload. Future crash dump support requires a dedicated privacy review.
