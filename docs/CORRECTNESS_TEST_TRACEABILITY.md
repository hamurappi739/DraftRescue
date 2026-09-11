# Correctness Invariant → Test Traceability

**Status:** implementation/release support map.

| Invariant | Required evidence |
|---|---|
| C-001 | TRK-006, PST-010, PTX-003 |
| C-002 | TRK-001, PST-002, PTX-007 |
| C-003 | OBS-003, E2E-005 |
| C-004 | PRF-002, PRF-003, PRF-007, PRF-014 |
| C-005 | MAT-001, MAT-002, MAT-003, RMF-001, RMF-002, RMF-003, RMF-004 |
| C-006 | UI-007 |
| C-007 | PTX-008 |
| C-008 | UI-004, E2E-010 |
| C-009 | RST-005, RST-006, PTX-013, E2E-009 |
| C-010 | ARC-004, PST-003, UI-002, UI-006 |
| C-011 | PST-007, E2E-002 |
| C-012 | PST-011, PTX-002, PTX-005, PTX-006 |
| C-013 | ERR-001, RST-005, RST-006 |
| C-014 | UI-007 |
| C-015 | PRF-001, PRF-002, PRF-013 |

| C-016 | OBS-005, OBS-006, OBS-007 |
| C-017 | OBS-008 |
| C-018 | OBS-004, PERF-005 |
| C-019 | SEC-012 |
| C-020 | SEC-011 |
| C-021 | EXP-001, EXP-002 |
| C-022 | OBS-008, PERF-004 |
| C-023 | OBS-006, OBS-007 |
| C-024 | CERT-002 |
| C-025 | FI-001, FI-002, PERF-006 |

| C-026 | CAP-001, CAP-002, CAP-003 |
| C-027 | CAP-005 |
| C-028 | SEC-022, P2F-006 |
| C-029 | SEC-015, SEC-016, CAP-004, P2F-005 |
| C-030 | ARC-007, P2F-001, P2F-002, P2F-003, P2F-004 |
| C-031 | SEC-022 |
| C-032 | CAP-008, P2F-006 |

## Rule

Each implemented invariant must have executable automated tests where practical. Manual/profile certification IDs supplement but do not replace unit/integration coverage for deterministic application logic.
