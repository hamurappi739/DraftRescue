# Codex Execution Guide

This document adapts the existing coding-agent handoff rules specifically for the Codex takeover.

## Principle

The repository is specification-first. Codex is an implementer and verifier of accepted contracts, not the default product designer.

## One task = one work package

Use `IMPLEMENTATION_WORK_PACKAGES.md`. A normal task should cover one `WP-x.y` only.

If implementation exposes a genuinely new architecture decision:

1. stop at the smallest safe point;
2. add the question to `OPEN_DECISIONS.md` if not already present;
3. run a focused experiment if possible;
4. record the accepted result as an ADR;
5. update tests/traceability;
6. resume only after the decision is explicit.

## Mandatory phase order

`0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10`

A phase gate is binary. Passing a phase does not authorize implementing the next phase in the same task.

## Source-of-truth precedence

1. explicit new user instruction;
2. product/privacy invariants in `DRAFTRESCUE_MASTER_CONTEXT.md` and invariant catalogs;
3. accepted ADRs;
4. phase-specific normative specifications;
5. general architecture/docs;
6. code comments/current implementation details.

If two lower-level documents conflict, do not choose silently. Surface the conflict.

## Required pre-change note

Before each coding change, state:

```text
Phase / WP:
Docs read:
Projects/files in scope:
P-* invariants:
C-* invariants:
Test IDs:
Open decisions involved:
Out of scope:
```

## Required post-change note

After each coding change, state:

```text
Files changed:
Build command/result:
Test command/result:
Test IDs executed/added:
Invariant coverage:
Experiment evidence:
Assumptions:
Open issues:
Stopped before next WP: yes/no
```

## Tests are part of implementation

A work package is not complete because the code compiles. Named test IDs and the phase exit criteria are part of the feature contract.

Do not weaken a privacy guard to satisfy a happy-path test.

## Privacy review questions for every PR/change

- Can this change read target text before security authorization?
- Can it persist plaintext or a reversible user-derived metadata string?
- Can it log user text or dynamic UI strings?
- Can an unknown/failure state be coerced into Allow?
- Can stale async work mutate newer state?
- Does it add history/revisions rather than one current state?
- Does it add a network path?
- Does it grant capture authority to Restore?
- Can an ambiguous target be forced?
- Does it silently broaden target/app support?

Any "yes" requires either rejection or a specifically approved ADR/spec change.

## Build environment truthfulness

The generation environment did not run real .NET builds. Codex must establish actual Windows/.NET 8 truth before implementation claims begin.

