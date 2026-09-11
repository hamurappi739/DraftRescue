# App Profile Specification

**Status:** architecture contract for Phase 7–9 and earlier test-target adapters.

## 1. Purpose

Different applications expose editable fields, completion, privacy, and restore capabilities differently. `AppProfiles` isolates those differences so the core engine does not become a collection of `if (processName == ...)` branches.

## 2. Profile responsibilities

A profile may describe:

- application identity matching;
- supported window/editor surfaces;
- metadata extraction rules;
- field eligibility hints;
- additional hard-deny contexts;
- completion/send/save signals;
- recovery fingerprint contributors;
- supported restore mechanism/capabilities;
- profile-specific test fixtures.

## 3. Profile non-responsibilities

A profile must not:

- weaken global `SecureInputGuard` hard denies;
- enable private browsing by accident;
- persist plaintext;
- write directly to storage;
- log field content;
- bypass retention;
- declare “all fields in this process are safe.”

Global deny wins over profile allow.

## 4. Conceptual shape

```text
AppProfile
- ProfileId
- DisplayName
- ProcessIdentityRules
- SupportedSurfaceRules
- PrivacyRules
- CompletionStrategy
- FingerprintStrategy
- RestoreCapabilities
- VersionCompatibility
```

Implementation may use interfaces/composition instead of one large class.

## 5. Capability model

Profiles should expose capabilities explicitly rather than forcing the caller to probe by exceptions.

Example:

```text
CanObserveMetadata
CanReadDraftSnapshot
CanDetectCompletion
CanDirectRestore
CanUseBrowserOrigin
SupportsPrivateModeDetection
```

A missing required capability means unsupported/deny for the dependent feature.

## 6. Initial profile order

Product priority remains:

1. Notepad as controlled Windows test target;
2. ordinary Windows text controls;
3. Chrome;
4. Edge;
5. Discord;
6. Telegram Desktop later.

Notepad is a test target, not proof that arbitrary editors are supported.

## 7. Version resilience

Profile logic should avoid fragile pixel coordinates and localized exact strings where stable accessibility/provider metadata exists.

Each profile should declare:

- versions/build families tested;
- assumptions about accessibility tree structure;
- fallback behavior when assumptions fail.

Failure to recognize a changed app version safely should degrade to Unsupported/Uncertain, not capture anyway.

## 8. Browser specialization

Chrome and Edge may share Chromium-oriented helpers, but they remain explicit profiles because private-mode indicators, process/window behavior, and release changes must be independently testable.

## 9. Profile test contract

Every production profile requires fixtures/manual integration scenarios for:

- ordinary safe compose field;
- password/login surface;
- payment/credential-like surface where applicable;
- close/reopen recovery;
- completion detection;
- mismatched field restore;
- app update/tree variation fallback;
- resource usage under idle and active typing.

## 10. Configuration vs code

Do not make security-critical selectors freely user-editable in MVP. App-profile internals are trusted product code/configuration shipped with DraftRescue.

Future advanced overrides require a separate threat review.


## Deterministic runtime resolution

Runtime profile selection follows `PROFILE_RESOLVER_ALGORITHM.md`. Unsupported/uncertified version or overlapping eligible profiles fail closed; profile file order and first-match behavior are forbidden. Recovery predicates follow `RECOVERY_EVIDENCE_LATTICE.md`.
