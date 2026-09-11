# Development Rules

## Small verified increments

Every phase is split into the smallest independently buildable and testable increment. Do not implement the next increment until the current one builds and its acceptance checks pass.

## Required completion report

Every implementation change should report:

1. scope completed;
2. files changed;
3. build command/result;
4. test command/result;
5. privacy/security impact;
6. unresolved risks;
7. confirmation that no extra scope was implemented.

## Coding principles

- Prefer explicit, small abstractions over framework-heavy architecture.
- Keep domain/application independent of Windows and Avalonia.
- Keep platform objects at the platform boundary.
- Do not create generic “event buses” carrying raw text.
- Do not expose draft text in `ToString()`/diagnostic representations when content-bearing models are introduced later.
- Cancellation and disposal will be required for future long-running observers.
- Any continuous observation must have measurable resource limits.

## Testing strategy

Phase 0 tests architecture and privacy-safe policy semantics.

Future phases should add:

- unit tests for policy and matching logic;
- adapter tests against controlled Windows targets;
- privacy regression tests for secure fields;
- retention/expiry tests;
- persistence encryption tests;
- restore-target safety tests;
- resource/performance smoke tests.
