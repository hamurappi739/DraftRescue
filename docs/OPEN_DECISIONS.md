# Open Decisions Register

This register separates **accepted direction** from choices that still require focused experiments/ADRs. A future coding agent must not silently resolve open items.

## Accepted direction (do not reopen casually)

- No global keyboard-hook typing stream; evaluate event-driven foreground/focus + targeted UI Automation first.
- Security/privacy classification is fail-closed.
- Prefer classification before reading text.
- Text reader requires a short-lived `AllowedFieldHandle`; arbitrary focused elements are not accepted.
- Context-generation tokens invalidate stale async observation/security/read results.
- Current draft snapshot replaces previous state; no durable revision history.
- Focus loss is not completion.
- Manual stable clear should remove obsolete active text rather than resurrect it later.
- Direct Restore is explicit, fail-closed, and requires fresh strong target matching.
- Ambiguous restore falls back to Preview/Copy; no force-restore MVP.
- Browser private/incognito contexts are denied by default.
- Retention is bounded; no forever/history mode.
- Correlation metadata uses versioned per-installation-keyed HMAC tokens where raw user-derived strings are unnecessary.
- MVP persistence baseline is SQLite current-state storage behind `IDraftRepository`; no WAL by default; no plaintext in repository.
- MVP protected payload format v1 uses Windows DPAPI/`ProtectedData` with `DataProtectionScope.CurrentUser` behind `IDraftProtector`.
- Retention is sliding from the latest eligible update; default 30 minutes; custom range 1 minute–24 hours; no forever mode.
- DraftRescue is a background tray utility; closing the main window does not quit; explicit Quit warns that new drafts will not be protected.
- Copy is explicit and DraftRescue does not automatically clear the clipboard in MVP.
- Normal periodic/current-state checkpoints are the durability mechanism; shutdown/session-end handling is bounded best-effort and must not block shutdown.
- DraftRescue does not auto-elevate or add a privileged helper/service for coverage in MVP.
- MVP app profiles ship with signed application releases; there is no remote profile-policy channel.
- Restore outcome is verified when a safe adapter capability exists; otherwise the result is explicitly unverified rather than falsely claimed successful.
- Exactly one observation/tracking/repository writer instance runs per interactive user session; local IPC is command-only/content-free in MVP.
- Supported target compatibility is tied to certified version/range; unknown/incompatible target versions fail closed for security-sensitive capabilities.

- Recovery matching uses a deterministic evidence lattice; no universal numeric confidence score/threshold.
- Recovery list is metadata-only; draft bodies are decrypted only for explicit Preview/Copy/authorized Restore.
- `SnapshotSequence` is persisted and stale repository writes are rejected transactionally.
- Profile resolution ambiguity fails closed instead of using first-match/file-order priority.
- Restore is single-flight per DraftId.
- Operational errors use stable content-free `DR-*` codes; provider exception messages are not blindly logged.

- Raw dynamic site/window/field labels are not persisted for Recovery-list decoration; cards use coarse presentation kind.

- Phase 1 event callbacks are content-free/non-blocking and only enqueue structural envelopes.
- Generic UIA property access is tiered/audited; dynamic text-like metadata is not prefetched before classification.
- Phase 1 performs zero target-content reads.
- Unknown editable fields are not allowed by default; eligibility requires a positive certified profile predicate.
- Browser forms are not generic early capture targets; early Chrome/Edge work is metadata/topology research only.
- Notepad is the first real-world reconnaissance target but remains Experimental until later certification.
- `AllowedFieldHandle` is a one-read, non-serializable capture capability; claim consumes it and each future read requires fresh classification.
- Capture capability is bound to context generation, ephemeral platform binding, profile revision and a monotonic 1000 ms default / 2000 ms hard deadline.
- Capture capability can never authorize Restore; Restore uses separate fresh target validation.
- Security policy is deterministic/pure after metadata signal collection; unknown/unavailable/failed required signals deny.
- Phase 2 DI graph contains no target-content reader.

## Still open / requires experiment or later ADR

1. Exact minimum supported Windows version.
2. Final measured Phase 1 event/API combination and numeric budgets. Baseline experiment uses foreground WinEvent + UIA focus + bounded reconciliation, but evidence may narrow the combination.
3. Exact secure-field detector signal set per native/Chromium/Electron framework beyond hard password/protected signals.
4. Exact private-browsing detection strategy per browser/version.
5. Exact snapshot acquisition strategy per control family after experiments (`ValuePattern`, `TextPattern`, app adapter, etc.).
6. Exact debounce/max-dirty-age values for persistence checkpointing.
7. Exact profile-specific strong-match predicates/anchors for each supported application after fixtures/measurement (global evidence-lattice semantics are accepted).
8. Direct restore mechanism per target/control family.
9. Packaging/update mechanism and exact Windows startup-registration mechanism.
10. Whether local diagnostic logs are enabled by default in release builds and their exact rotation quota.

Each item should be resolved by a narrow experiment or ADR and accompanied by tests. Unsupported uncertainty must fail closed rather than trigger a broad fallback.
