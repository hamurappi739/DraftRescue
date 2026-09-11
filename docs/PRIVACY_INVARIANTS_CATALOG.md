# Privacy and Safety Invariants Catalog

These invariants are intended to become automated tests, architecture tests, code-review checklist items, and Cursor stop conditions.

## P-001 No general keystroke stream

DraftRescue must not create a global durable or in-memory stream representing arbitrary keyboard input across applications.

## P-002 Classify before content read

Security/private/support classification operates on metadata first. Text-reading API requires an allowed capability.

## P-003 Fail closed

Timeout, provider error, unknown private mode, unsupported control, stale identity, or conflicting security evidence never becomes `Allowed` by fallback.

## P-004 Password hard deny

Any trusted protected-content/password signal immediately denies capture.

## P-005 Credential/banking deny

Credential-, PIN-, security-code-, card-, and banking-like fields are outside supported capture even when a provider does not set a password flag.

## P-006 Private browsing deny

Supported browser private modes are denied by default before draft text read.

## P-007 No cloud text

No draft content, preview, clipboard content, or fingerprint source string is sent to network/cloud/AI services.

## P-008 No plaintext logs

Normal logs, traces, crash diagnostics, metrics, and exceptions never include draft content.

## P-009 At-rest protection

Repository receives protected payload; no plaintext draft field exists in durable schema.

## P-010 Current snapshot only

One logical draft has one current persisted payload. Text changes update, not append durable revisions.

## P-011 Bounded retention

Every recoverable draft has expiry. No forever/history setting in MVP.

## P-012 Metadata minimization

Raw titles, URLs, labels, and accessibility names are not persisted when keyed fingerprints suffice.

## P-013 Restore revalidation

Restore requires current target location, fresh security classification, and StrongMatch. Old capture eligibility does not authorize restore.

## P-014 No force restore

Ambiguous/NoMatch cannot be overridden by generic UI "force" in MVP.

## P-015 Clipboard is explicit

Clipboard receives draft text only after explicit Copy user action.

## P-016 UI least privilege

Desktop UI does not directly access repository, DPAPI, UIA platform objects, or generic decrypt APIs.

## P-017 Unknown profile is not permissive

Unknown/unvalidated app or incompatible profile version is unsupported/uncertain unless a separately validated generic profile applies.

## P-018 Expired means unavailable

Expired records are excluded from recovery even if cleanup has not physically deleted the row yet.

## P-019 No sensitive test fixtures in repository

Tests use synthetic obvious fake secrets. Never commit real user drafts, credentials, browser data, or captured production dumps.

## P-020 Support bundle is content-free

Any future diagnostic export must exclude protected payloads by default and must never include plaintext content.

## P-021 No privilege escalation for coverage

DraftRescue does not auto-elevate, install a privileged text-observation service, or inject a privileged helper merely to observe/restore higher-integrity applications. Unsupported integrity boundaries fail closed.

## P-022 Shutdown is non-blocking

Draft protection depends on normal periodic checkpoints. Session-end handling remains bounded and must not hold Windows shutdown/logoff indefinitely to save draft content.

## P-023 MVP profiles are release-bound

Security-sensitive app profile rules ship with the signed application release in MVP. No remote profile channel may silently broaden capture behavior.

## P-024 Restore outcome is conservative

DraftRescue claims verified Restore success only when the target adapter can safely verify the resulting target state. Unverified writes are represented explicitly and do not silently destroy the sole recoverable copy.

## P-025 Content-free event callbacks

Foreground/focus callbacks carry only bounded structural identifiers/timestamps and never read or enqueue target text or dynamic UIA strings.

## P-026 UIA property allowlist before classification

Generic pre-classification UIA access is limited to audited structural properties. Dynamic text-like metadata and content-bearing properties are not opportunistically prefetched.

## P-027 Browser form capture requires certified positive safety evidence

Generic browser editability is insufficient. Browser form capture remains denied until private-mode and sensitive-purpose exclusion are proven for that browser/version before text read.

## P-028 Phase 1 reads zero target content

Active App + Field Detection is metadata-only. Its production and experiment paths do not read ValuePattern/TextPattern/LegacyIAccessible text or equivalent target content.

## P-029 Experiment evidence is content-free

Performance/reliability experiments may record counts, timings, enums, booleans and audited fingerprints, but not raw titles, URLs, UIA dynamic strings, field content, clipboard content, or accessibility-tree dumps.

## P-030 Certification uses synthetic/non-sensitive evidence

Target certification and fault testing use synthetic canaries and metadata-only evidence. Real user drafts, browser autofill data, credentials, payment data, or production accessibility dumps are not test inputs.

## P-031 One-read capture capability

Each `AllowedFieldHandle` authorizes at most one target-content read attempt. Reuse requires fresh metadata classification and a new capability.

## P-032 Capture capability is ephemeral and non-durable

Allowed-field capabilities are process-local, non-serializable, non-persisted and non-loggable. They cannot survive application restart or be reconstructed from strings/storage.

## P-033 Capture permission never authorizes Restore

A capability issued for reading a draft cannot authorize writing/restoring text. Restore requires its own fresh security/matching validation and separate target capability.

## P-034 Required security unknown blocks permission

A required security signal that is unknown, unavailable, failed or stale is never coerced to a safe false value.

## P-035 Security gate has no content-reader dependency

Phase 2 security collection/evaluation/issuance code does not depend on a target-content reader, tracker, repository, protector, clipboard or restore service.

## P-036 Positive allow is profile/version bound

Ordinary editability and `IsPassword=false` never grant permission by themselves. Allow requires a certified, product-owned positive predicate for the resolved profile/version.

## P-037 Content read requires consumed capability

No target-content provider access occurs unless a fresh `AllowedFieldHandle` has been atomically claimed for that read attempt.

## P-038 Bounded target-content acquisition

TextPattern reads are finite; oversized content is rejected rather than silently truncated. ValuePattern is limited to certified bounded surfaces.

## P-039 Phase 3 plaintext stays transient

Phase 3 plaintext is limited to provider temporary data, transient snapshot, and one current in-memory tracker value. It never enters logs, diagnostics, UI, clipboard, persistence, or network.

## P-040 No generic content fallback ladder

Failure of the certified read strategy does not trigger LegacyIAccessible, keyboard, clipboard, or opportunistic alternate content reads.

## P-041 Stale/failed reads cannot mutate draft state

Timeout, provider failure, cancellation, stale generation, target change, or out-of-order result cannot create/update/clear the current draft.

## P-042 Exact-content preservation

Generic capture does not trim, normalize Unicode, rewrite line endings, correct spelling, or otherwise semantically modify user text.

## P-043 Protect before persistence

No durable repository call may receive or derive plaintext; protection succeeds before protected-record construction/storage.

## P-044 Per-user protection scope

MVP durable draft/installation-secret protection uses DPAPI CurrentUser, never LocalMachine or plaintext fallback.

## P-045 Installation secret stays secret

The random fingerprint HMAC key is separately protected, non-exported, non-logged and never stored plaintext in DB/settings.

## P-046 No plaintext storage side channel

Database, rollback journal, temp files, settings, logs and quarantine artifacts contain no intentional plaintext draft body.

## P-047 Metadata listing does not decrypt

Recoverable-list/startup metadata queries do not select/decrypt draft bodies.

## P-048 Corruption does not become extraction

Corrupt/incompatible storage never triggers raw-page/string salvage or automatic upload.

## P-049 Deletion claims stay conservative

Product copy never equates logical/SQLite secure deletion with guaranteed forensic erasure on all storage layers.

## P-050 Unknown store version is fail-closed

Unknown/newer schema or protection versions are not auto-dropped, guessed or reinterpreted to preserve convenience.


## Enforcement map

Each implementation PR/work package must list affected invariant IDs. Tests should use these IDs in names/categories where practical so failures map back to product policy.
