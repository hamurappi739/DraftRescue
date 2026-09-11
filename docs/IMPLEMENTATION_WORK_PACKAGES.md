# Implementation Work Packages

**Purpose:** future Cursor tasks should reference one work package at a time. Each package has a hard stop.

# Phase 0

## WP-0.1 — Windows build validation

**Input:** current repository.

**Do:** run verify script on Windows .NET 8, fix only compile/package/test issues.

**Do not:** add runtime observation.

**Pass:** restore/build/tests green; shell launches.

## WP-0.2 — Architecture dependency audit

**Do:** verify references/namespaces against architecture and add architecture tests only if needed.

**Pass:** no Domain/Application platform leakage.

# Phase 1 — metadata observation only

## WP-1.1 — Foreground application prototype

Create the smallest `Platform.Windows` adapter that emits foreground HWND/process/application identity changes.

No UIA field inspection. No text.

Tests: normalization + resource cleanup; manual switch-app script.

## WP-1.2 — Focused candidate metadata prototype

Add focused element discovery for Notepad/test harness using UI Automation metadata only.

Capture safe capability fields: control role/type, editability, read-only/protected flag, stable IDs where available.

Do not call Value/TextPattern text retrieval.

## WP-1.3 — Observation coordinator

Compose foreground + focused candidate metadata into a cancellable metadata-only stream.

Coalesce duplicates; skip own process; expose unsupported/transient states.

## WP-1.4 — Performance/leak measurement

Run focus-switch cycles and record CPU/event/handle behavior. Fix leaks only.

**Phase 1 stop.**

# Phase 2 — security gating

## WP-2.1 — Security metadata model

Define typed security signals and policy result. No text reader.

## WP-2.2 — Native password deny

Implement and test `IsPassword`/protected signal deny for controlled native fixtures.

## WP-2.3 — Failure/unknown deny

Provider exception, missing required metadata, element replacement, timeout => deny.

## WP-2.4 — Credential/financial profile metadata

Add only tested metadata/profile rules for controlled fixtures. Do not inspect text content.

## WP-2.5 — Allowed-field capability token

Introduce a short-lived type/token that can be created only after allow and is required by future text reader.

**Phase 2 stop.**

# Phase 3 — current draft tracking

## WP-3.1 — Eligible snapshot reader for one controlled target

Read current text only from an `AllowedFieldHandle`. Start with one proven control family.

## WP-3.2 — Draft lifecycle reducer/state machine

Implement Active/current snapshot, stable clear, context loss -> Recoverable, completion evidence policy.

Pure tests first.

## WP-3.3 — Snapshot coalescing in memory

Update current state without revision history. Define measured debounce/max dirty age candidates but do not add durable storage.

## WP-3.4 — Controlled close/reopen scenario

Demonstrate current draft becomes recoverable in memory when target disappears unexpectedly.

**Phase 3 stop.**

# Phase 4 — encrypted persistence

## WP-4.1 — Contracts + protected record/schema

Implement only Phase-4 contracts, `ProtectedDraftRecordV1`, schema constants and architecture tests. No database/DPAPI implementation yet.

## WP-4.2 — DPAPI + installation secret

Implement Windows `CurrentUser` protector and the DPAPI-protected random installation HMAC secret. No LocalMachine/fallback.

## WP-4.3 — SQLite bootstrap + schema v1

Implement per-user store, validated PRAGMAs, schema creation/version check and metadata-only list query.

## WP-4.4 — Monotonic protected repository + writer queue

Implement atomic sequence-aware upsert and one logical writer queue. No revision table.

## WP-4.5 — Checkpoint + retention execution

Wire measured Phase-3 scheduler to protector-before-repository flow; startup/runtime/action-time expiry remains metadata-only.

## WP-4.6 — Corruption + migration

Fail-closed incompatible/corrupt store handling, quarantine policy and transactional known migrations. No salvage.

## WP-4.7 — Plaintext-at-rest certification + fault tests

Canary scans, kill/crash/lock/disk-full/stale-sequence tests.

## WP-4.8 — Exit gate

Run all Phase-4 gates and stop before Preview/UI decryption.

**Phase 4 stop.**

# Phase 5 — recovery UI

## WP-5.1 — Recoverable list query/ViewModel

Metadata only; no body/snippet decryption on list load. Preview is the explicit content-reveal path.

## WP-5.2 — Preview

Explicit decrypt/display path; no clipboard/logs.

## WP-5.3 — Discard

Single + discard-all confirmation semantics; permanent delete.

## WP-5.4 — Copy security review and implementation

Only after explicit clipboard behavior decision.

**Phase 5 stop.**

# Phase 6 — restore

## WP-6.1 — Matching evidence model

Implement categorical/profile-aware matching as pure policy with fixtures.

## WP-6.2 — Live target rematch

Locate current candidate and re-run secure guard before any write.

## WP-6.3 — First direct restore adapter

One target/control family only. Prefer supported programmatic value-setting API.

## WP-6.4 — Race/result handling

Target changes, target closes, mutation fails, verification succeeds/fails.

## WP-6.5 — Restore lifecycle cleanup

Confirmed success deletes recoverable payload; failure keeps it.

**Phase 6 stop.**

# Phase 7 — browsers

## WP-7.1 — Chrome research fixture

Map accessibility tree/capabilities for normal vs Incognito using synthetic pages only.

## WP-7.2 — Chrome private-mode hard deny

Must pass before ordinary Chrome capture is enabled.

## WP-7.3 — Chrome supported editor profile

One known ordinary page editor scenario; login/payment/browser chrome negative cases.

## WP-7.4 — Chrome recovery + cross-origin restore tests

## WP-7.5..7.8 — Repeat independently for Edge

**Phase 7 stop.**

# Phase 8 — Electron

## WP-8.1 — Discord accessibility/profile research
## WP-8.2 — secure/login negative cases
## WP-8.3 — compose tracking/completion
## WP-8.4 — recovery/restore

Telegram Desktop is not bundled into this phase automatically.

# Phase 9 — profile hardening

## WP-9.1 — shared profile contracts/capabilities
## WP-9.2 — version compatibility/fail-closed rules
## WP-9.3 — profile certification matrices

# Phase 10 — hardening

## WP-10.1 — privacy canary suite
## WP-10.2 — race/failure injection suite
## WP-10.3 — resource soak/leak tests
## WP-10.4 — packaging/startup/update ADRs
## WP-10.5 — final threat-model review

# Rule

A Cursor request should normally contain **one WP only**. If a WP reveals a new architecture decision, stop and resolve it before continuing.
