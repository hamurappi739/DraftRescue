# Phase 3 Fault and Negative Matrix

| Case | Expected result | Tracker mutation | New permission auto-issued? |
|---|---|---:|---:|
| expired handle | Expired | 0 | no |
| already consumed | AlreadyConsumed | 0 | no |
| generation changed before read | Revoked/Stale | 0 | no |
| target disappears | TargetChanged/ProviderFailure | 0 | no |
| UIA timeout | Timeout | 0 | no |
| TextPattern > configured limit | TooLarge | 0 | no |
| ValuePattern > configured limit | TooLarge | 0 | no |
| unsupported pattern | UnsupportedReadStrategy | 0 | no |
| late old read after newer read | Success then StaleIgnored | old=0 | no |
| provider returns empty once | Empty candidate | no terminal clear | no |
| provider error | ProviderFailure | 0 | no |
| cancellation | Cancelled/late discarded | 0 | no |

Every failure path also asserts zero persistence/clipboard/network/UI-body side effects.
