# Future Cursor Execution Protocol

**Status:** mandatory handoff protocol once the user explicitly chooses to introduce Cursor. Current project mode remains no-Cursor.

## 1. One work package per task

A normal Cursor task names exactly one `WP-*`. It may perform the minimum refactor required inside that boundary, but it must not pre-build later phases.

## 2. Required preamble from Cursor before edits

Cursor states:

```text
Work package:
Files/projects expected to change:
Required docs read:
P-* invariants:
C-* invariants:
Named acceptance tests:
Explicit out-of-scope:
Open decision blocking work: none / <decision>
```

If blocked by an open decision, Cursor stops before broad implementation.

## 3. Implementation behavior

- use the canonical names/contracts unless a compile/API constraint requires a narrow change;
- never weaken security/privacy for convenience;
- no catch-all fallback that captures/reads/writes more broadly;
- do not add third-party packages without explaining necessity and scope;
- do not change accepted ADRs as incidental cleanup;
- never log/copy fixture plaintext outside synthetic tests;
- do not mark experimental profile `supported` without certification evidence.

## 4. Required completion report

```text
Work package completed:
Files changed:
Build command/result:
Test command/result:
Tests added/changed by ID:
P-* / C-* invariants covered:
Scope deviations: none / ...
New assumptions: none / ...
Open issues:
Stopped before next WP: yes
```

## 5. Review rule

ChatGPT/product owner reviews the result against work-package acceptance gates before the next Cursor task is issued.
