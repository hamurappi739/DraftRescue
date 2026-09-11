# Phase 1 Exit Criteria — Active App + Field Detection Research

Phase 1 is not complete because events appear to fire. It exits only when the observation plane is bounded, content-free, deterministic enough for Phase 2, and measured on representative synthetic/real targets.

## Required pass criteria

### Architecture

- Windows observation implementation remains inside Platform.Windows.
- Application receives only normalized content-free candidate metadata.
- no keyboard hook/raw-input typing stream exists.
- event callbacks do not call persistence/UI directly.

### Privacy

- Phase 1 text-reader invocation count is zero in all experiment paths.
- Tier C/D UIA properties are absent from generic pre-classification cache/query code.
- no raw dynamic UIA strings appear in logs/experiment artifacts.
- own-process and incompatible-integrity targets are ignored/unsupported.

### Correctness

- foreground/focus event routing survives duplicate/out-of-order signals;
- stale reconciliation cannot replace a newer context;
- event storm coalescing converges on latest current context;
- normalization is deterministic under table tests;
- registration/disposal is idempotent.

### Reliability/performance

- idle path does not poll continuously;
- callbacks meet the agreed bounded-duration budget;
- UIA provider hang does not freeze Avalonia UI or session-end path;
- 30-minute focus-switch soak shows no unbounded handle/thread/queue growth;
- backoff prevents hot retry loops.

### Evidence

- synthetic Win32 field run;
- synthetic managed field run;
- Notepad reconnaissance run;
- at least one deliberately failing/hung provider case;
- machine-readable experiment records pass content-safety audit.

## Explicit non-goals

Phase 1 does **not** need to:

- read or save a draft;
- classify all credentials;
- support browsers;
- restore text;
- create SQLite/DPAPI runtime implementation.

## Gate

Only after review of this evidence may Phase 2 implement `SecureInputGuard`. Phase 2 still must not persist drafts.
