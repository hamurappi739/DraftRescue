# Phase 2 Source Guard Specification

`scripts/phase2_content_boundary_guard.ps1` is a defense-in-depth gate for the future Phase 2 implementation.

It scans Application/Platform.Windows source for obvious target-content and keyboard/clipboard shortcuts. It is intentionally phase-scoped: Phase 3 will legitimately introduce a capability-gated content reader and must replace this broad rule with a more precise architecture test rather than disabling privacy checks entirely.

The source scan does not prove safety by itself. Required proof also includes DI graph tests, typed capability-only APIs, negative fixture spies, and invariant/test traceability.
