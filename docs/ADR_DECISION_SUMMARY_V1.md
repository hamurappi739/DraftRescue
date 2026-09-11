# Architecture Decision Summary v1

This is a compact future-agent checklist. Full rationale lives in `adr/` and specifications.

| ADR | Decision |
|---|---|
| 0001 | Layered architecture with Windows-specific platform isolation |
| 0002 | Capture eligibility is fail-closed |
| 0003 | No general keystroke stream |
| 0004 | Manual/lightweight composition in Phase 0 |
| 0005 | Event-driven foreground/focus observation before keyboard hooks |
| 0006 | Restore is user-initiated and fail-closed |
| 0007 | Persist current snapshot, not revision history |
| 0008 | Context metadata fingerprints use installation-keyed HMAC |
| 0009 | SQLite current-state store; no WAL baseline |
| 0010 | DPAPI CurrentUser direct payload protection for format v1 |
| 0011 | Text-reading APIs require short-lived allowed capability |
| 0012 | Context-generation tokens invalidate stale async results |
| 0013 | Sliding retention, 30-minute default, custom 1 minute–24 hours |
| 0014 | Background tray lifecycle; close window != quit |
| 0015 | Explicit Copy only; no automatic clipboard clear |
| 0016 | Desktop-wide UI Automation runs on dedicated non-UI MTA worker |
| 0017 | Normal periodic checkpoints provide durability; shutdown-time save is bounded best-effort |
| 0018 | No privileged helper or automatic elevation in MVP |
| 0019 | MVP app profiles ship with signed application releases; no remote policy channel |
| 0020 | Restore success is verified when safely possible; unverified outcome stays explicit |
| 0021 | Exactly one observer/writer instance per interactive user session |
| 0022 | Unknown/incompatible target versions do not inherit security-sensitive support |
| 0023 | Recovery list is metadata-only; no automatic body decryption/snippets |
| 0024 | Recovery matching uses deterministic evidence lattice, not global numeric score |
| 0025 | Persist SnapshotSequence and reject stale writes transactionally |
| 0026 | Ambiguous eligible profile resolution fails closed |
| 0027 | Restore is single-flight per DraftId |
| 0028 | Persisted content reveal is explicit and narrowly scoped |
| 0029 | Operational failures use stable content-free error codes |
| 0030 | Recovery list persists no raw dynamic site/window/field labels for presentation |

| 0031 | Observation callbacks are content-free/non-blocking; work is deferred |
| 0032 | UIA properties are acquired by audited safety tier |
| 0033 | Event storms coalesce toward latest context; no focus-event history |
| 0034 | Editable + IsPassword=false is not an allow decision |
| 0035 | Phase 1 performs zero target-content reads |
| 0036 | UIA cache requests use explicit audited allowlists |
| 0037 | Browser forms are not generic early capture targets |
| 0038 | Notepad starts as experimental reconnaissance target, not Supported |

| 0039 | `AllowedFieldHandle` is a one-read capture capability |
| 0040 | Capture capability is generation/binding/profile-bound with 1000 ms default and 2000 ms hard monotonic age cap |
| 0041 | Security policy evaluation is pure/deterministic after metadata signal collection |
| 0042 | Phase 2 security DI graph contains no target-content reader |
| 0043 | Positive allow predicates are profile-bound and target-version-certified |
| 0044 | Unknown/unavailable/failed required security signal is not coerced to false |
| 0045 | Capture capability cannot authorize Restore writes |

| 0046 | Phase 3 target-content read requires an atomically consumed one-read capability. |
| 0047 | Production TextPattern reads always use a finite maxLength (`configuredLimit + 1`); `GetText(-1)` is forbidden. |
| 0048 | ValuePattern content reads are allowed only for explicitly certified bounded single-line surfaces. |
| 0049 | No generic LegacyIAccessible/keyboard/clipboard fallback is used to recover content when the certified UIA read strategy fails. |
| 0050 | Plaintext is transient and copy-minimized; DraftRescue does not claim reliable zeroization of managed strings. |
| 0051 | Oversized target text returns a typed `TooLarge` result and never becomes a silently truncated draft. |
| 0052 | The generic capture path preserves target text exactly; no trimming, Unicode normalization, newline rewriting, or semantic cleanup. |
| 0053 | Phase 3 tracker stores one current in-memory snapshot per logical draft and no revision history. |
| 0054 | A single empty read does not terminally clear a tracked draft; empty state requires a second authorized observation and monotonic stabilization baseline. |

| 0055 | Repository boundary accepts protected records only; protector runs before repository |
| 0056 | Phase-4 DPAPI v1 uses CurrentUser and never plaintext fallback |
| 0057 | SQLite DELETE journal + synchronous EXTRA + secure_delete ON is the Phase-4 baseline |
| 0058 | WAL is not used in MVP without a new privacy/performance ADR |
| 0059 | Persistence mutations use one serialized in-process writer |
| 0060 | Fingerprints use a separate random 256-bit DPAPI-protected installation HMAC secret |
| 0061 | Recoverable-list query is metadata-only and never loads/decrypts bodies |
| 0062 | Corrupt storage has no plaintext/raw-page salvage mode in MVP |
| 0063 | SQLite secure_delete is defense-in-depth, not a forensic wipe promise |

Cursor must not reopen accepted ADRs inside unrelated work packages.
