# Persistence Transaction Test Matrix

| ID | Scenario | Expected |
|---|---|---|
| PTX-001 | insert first sequence | one valid row, Inserted |
| PTX-002 | update sequence N -> N+1 | same row, payload/metadata/times/sequence move atomically |
| PTX-003 | late N after N+1 committed | StaleIgnored; row remains N+1 |
| PTX-004 | retry exact N with equivalent record | IdempotentNoChange |
| PTX-005 | fault before commit | old valid row survives |
| PTX-006 | fault after commit acknowledgement boundary | new complete row visible |
| PTX-007 | hundreds of updates | one row per DraftId, no revision table growth |
| PTX-008 | delete missing DraftId | idempotent already-absent success |
| PTX-009 | delete expired batch | no decryptor invocation |
| PTX-010 | discard-all | no decryptor invocation, bounded transaction |
| PTX-011 | malformed protected row | typed corruption/incompatible result; no plaintext salvage |
| PTX-012 | DB busy/locked | bounded wait then typed failure/backoff; no infinite UI hang |
| PTX-013 | verified Restore then delete fails | no automatic second target write; deletion may retry |
| PTX-014 | migration fails | original state preserved where possible; feature fails closed |
| PTX-015 | plaintext canary | absent from raw DB/journal/temp files under tested configuration |
