# ADR 0062 — No storage salvage mode in MVP

**Decision:** corrupt/incompatible stores fail closed and may be quarantined locally; DraftRescue does not scan raw pages/strings or upload the database to salvage text.

**Reason:** salvage tooling creates a powerful data-extraction surface and undermines the encrypted-storage boundary.
