# Machine-readable specifications

These files support future implementation/test tooling. Human-readable policy remains in `../docs`.

- `app-profile.schema.json` — app-profile manifest schema.
- `protected-draft-record.schema.json` — logical protected record shape; documentation/test fixture use.
- `privacy-invariants.v1.json` — stable privacy invariant IDs and short machine-readable descriptions.
- `settings.schema.json` — bounded MVP settings schema.
- `context-fingerprint-set.schema.json` — logical versioned fingerprint set shape.
- `examples/app-profile.template.json` — schema-valid example only; not a real supported application profile.
- `examples/settings.default.json` — schema-valid default settings example.

Rules:

- schema validity does not imply security approval;
- app profiles cannot weaken global invariants;
- no real user text or production dumps belong under `specs/examples`.


- `error-codes.v1.json` — stable content-free operational error catalog.
- `correctness-invariants.v1.json` — machine-readable `C-*` correctness invariant index.
- `recovery-evidence.schema.json` — documentation schema for categorical recovery evidence fixtures.
- `use-case-contracts.v1.json` — machine-readable use-case/result catalog.
- `recovery-match-fixtures.v1.json` — initial table-driven categorical recovery fixtures.
- `examples/app-profile.synthetic-supported.json` — synthetic schema-valid supported profile with certified version range.
- `examples/protected-draft-record.example.json` — protected-record shape including SnapshotSequence.
- `examples/recovery-evidence.strong.example.json` — sample evidence object.
- `correctness-traceability.v1.json` — C-* to test-ID mapping.
## Phase 2

- `security-evidence.schema.json` — structural metadata-only evidence shape.
- `phase2-security-fixtures.v1.json` — deterministic allow/deny policy fixtures.
- `allowed-field-capability-test-fixtures.v1.json` — synthetic capability lifecycle scenarios; not a serialization format.
- `phase2-test-matrix.v1.json` — Phase 2 release-gate test IDs.
- `phase2-architecture-rules.v1.json` — machine-readable boundary constants/denylist.
