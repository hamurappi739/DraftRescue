# Phase 3 Implementation Boundary

## Allowed

Phase 3 may add:

- `IEligibleFieldTextReader` abstraction;
- Windows reader strategies for explicitly certified UIA patterns;
- one-shot capability claim/consume integration;
- bounded target-content reads;
- transient `FieldTextSnapshot`;
- in-memory one-current-snapshot tracker;
- typed empty/clear candidate behavior;
- test-only fake readers and synthetic target fixtures.

## Forbidden

Phase 3 must not add:

- persistence or database packages;
- DPAPI/protection;
- recovery UI/content preview;
- clipboard;
- Restore writes;
- browser generic capture;
- raw keyboard or Raw Input hooks;
- cloud/network text path;
- full input history/revision list;
- unbounded `TextPatternRange.GetText(-1)`;
- generic `LegacyIAccessible.Value` fallback;
- logging/metrics/crash diagnostics containing snapshot text.

## Structural rule

`Platform.Windows` may produce a transient content result only after receiving a successfully claimed capture capability. `Application` owns tracking semantics. `Domain` does not reference UIA objects or raw platform handles.

## Phase exit

Phase 3 is complete only when a synthetic allowed field can update an in-memory current draft and all negative/stale/oversize paths prove they cannot mutate tracker state.
