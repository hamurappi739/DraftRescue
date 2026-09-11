# Phase 1 Fault and Performance Matrix

| ID | Scenario | Expected result |
|---|---|---|
| OBS-004 | callback receives focus/foreground | envelope has no dynamic text; no deep UIA call in callback |
| OBS-005 | duplicate foreground+focus for same logical target | bounded reconcile; generation does not churn unnecessarily |
| OBS-006 | 1000-event burst | backlog remains bounded; latest reconcile preserved |
| OBS-007 | context A resolves after B became current | A result discarded |
| OBS-008 | same raw metadata in reordered dictionaries/events | same normalized candidate |
| OBS-009 | DraftRescue UI gets focus | ignored as own process |
| OBS-010 | target exits before reconcile | typed unavailable result; no stale candidate |
| SEC-009 | generic candidate exposes Name/HelpText | generic pre-classifier does not request/log them |
| SEC-010 | unknown editable web form | denied/unsupported before text read |
| SEC-011 | allowed capability from generation N used in N+1 | rejected |
| SEC-012 | password hard-deny plus positive allow hints | deny wins independent of evaluation order |
| SEC-013 | required secure signal unavailable | deny/uncertain, never Allowed |
| PERF-004 | Tier A cache batch | bounded property count and measured latency |
| PERF-005 | callback synthetic load | callback p99 within approved budget, no blocking provider traversal |
| PERF-006 | repeated provider timeout | bounded exponential backoff; no hot loop |
| PERF-007 | 30-minute focus soak | bounded handles, threads, memory, queue depth |
| FI-001 | hung provider | UI responsive; worker timeout/backoff path activates |
| FI-002 | stale UIA element | typed stale/unavailable result; no fallback to permissive |
| FI-003 | incompatible integrity/elevation | unsupported before content read |
| FI-004 | provider throws message containing canary | logs contain error code, not raw exception message/canary |
| EXP-001 | Phase 1 experiment record | content-safety audit all forbidden fields false |
| EXP-002 | all Phase 1 scenarios | text-reader invocation count == 0 |
| CERT-001 | Notepad reconnaissance matrix | evidence complete but status remains experimental |
| CERT-002 | one required negative case omitted | promotion to supported is blocked |
