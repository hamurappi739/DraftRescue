# ADR 0058 — WAL is not an MVP storage mode

**Decision:** do not enable WAL in MVP.

**Reason:** DraftRescue has a tiny single-writer workload and prioritizes data minimization; WAL would add a durable side file and older encrypted page versions until checkpoint/reuse. Any future switch requires an explicit privacy/performance ADR.
