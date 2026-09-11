# Application Orchestration State Machine

**Status:** accepted coordination model. It describes orchestration, not UI state.

## 1. Per-current-context states

```text
NoContext
  -> MetadataCandidate
  -> Classifying
  -> Denied | AllowedHandleReady
  -> SnapshotPending
  -> TrackingCurrent
  -> ContextLost
```

Every context transition carries `ContextGeneration`. Any async completion from an older generation is ignored.

## 2. `NoContext`

No supported foreground target is currently active. Existing recoverable drafts remain durable independently.

## 3. `MetadataCandidate`

Only application/window/field metadata exists. No draft text has been read.

Transitions:

- profile unsupported -> `Denied`/`NoContext`;
- candidate absent -> `NoContext`;
- metadata ready -> `Classifying`.

## 4. `Classifying`

Runs app support, secure/private, editability, integrity, and policy gates.

- any deny/unknown/error -> `Denied`;
- `Allowed` -> mint short-lived handle -> `AllowedHandleReady`.

No plaintext exists in the application pipeline before this transition succeeds.

## 5. `AllowedHandleReady`

A bounded change/snapshot trigger may request content using the capability. Context change/TTL expiry invalidates it.

## 6. `SnapshotPending`

Eligible read is in flight for generation N/sequence S.

Results:

- stale generation -> discard;
- empty/stable clear -> tracker clear logic;
- plaintext snapshot -> tracking path;
- timeout/provider failure -> no permissive fallback.

## 7. `TrackingCurrent`

Tracker owns current logical draft. Repeated snapshots replace current state via increasing sequence. Checkpoint scheduler protects/persists current state according to debounce/max-dirty-age policy.

## 8. `ContextLost`

Loss does not mean submitted. Completion detector/profile rules evaluate evidence:

- strong completion -> terminal removal;
- stable user clear -> terminal removal;
- unknown/context destroyed -> keep last recoverable checkpoint until retention;
- switch to another field -> start a new generation.

## 9. Independent recovery workflow

Recovery list/Preview/Copy/Restore is not a state of the active observation pipeline. A user can recover an older durable draft while observation of another allowed field exists. Restore itself is single-flight per DraftId and performs fresh classification/matching.

## 10. Safety invariant

There is no transition `Denied -> ReadAnyway`, `ProviderError -> Allowed`, or `AmbiguousRestore -> ForceWrite`.
