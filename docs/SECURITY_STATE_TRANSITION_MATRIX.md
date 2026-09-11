# Security / Capability State Transition Matrix

| From | Event | To | Capability effect |
|---|---|---|---|
| Candidate | evidence collection succeeds | Evaluating | none |
| Candidate | timeout/stale/access failure | Denied | none |
| Evaluating | any hard deny | Denied | none |
| Evaluating | allow predicate absent/unknown | Denied | none |
| Evaluating | complete positive proof | AllowedDecision | none yet |
| AllowedDecision | binding/generation still current | CapabilityFresh | issue one |
| AllowedDecision | binding/generation changed | Denied/Stale | none |
| CapabilityFresh | monotonic deadline passes | CapabilityRevoked | unusable |
| CapabilityFresh | generation/profile/binding changes | CapabilityRevoked | unusable |
| CapabilityFresh | atomic future read claim | CapabilityConsumed | exactly one claim |
| CapabilityConsumed | any retry | CapabilityConsumed | reject |
| CapabilityRevoked | any claim | CapabilityRevoked | reject |

There is no transition from Denied to CapabilityFresh without a completely new evidence collection/evaluation cycle.
