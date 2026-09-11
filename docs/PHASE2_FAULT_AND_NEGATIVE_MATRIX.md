# Phase 2 Fault & Negative Matrix

Every row must result in **no capability** and `ContentReadInvocationCount == 0`, except the explicit positive-control row.

| Case | Condition | Expected result | Capability | Content reads |
|---|---|---|---:|---:|
| P2-01 | `IsPassword=true` | SecureField deny | 0 | 0 |
| P2-02 | protected signal unknown | UncertainSecureState deny | 0 | 0 |
| P2-03 | protected signal unavailable | UncertainSecureState deny | 0 | 0 |
| P2-04 | provider timeout | ProviderUnavailable deny | 0 | 0 |
| P2-05 | element stale | StaleContext deny | 0 | 0 |
| P2-06 | profile missing | UnsupportedProfile deny | 0 | 0 |
| P2-07 | profile ambiguous | UnsupportedProfile deny | 0 | 0 |
| P2-08 | target version unknown | UnsupportedProfile deny | 0 | 0 |
| P2-09 | integrity inaccessible | UnsupportedContext deny | 0 | 0 |
| P2-10 | private mode confirmed | PrivateBrowsing deny | 0 | 0 |
| P2-11 | required private mode unknown | UncertainPrivateMode deny | 0 | 0 |
| P2-12 | credential-purpose fixture | CredentialLikeField deny | 0 | 0 |
| P2-13 | PIN/OTP fixture | CredentialLikeField deny | 0 | 0 |
| P2-14 | payment fixture | BankingOrFinancialField deny | 0 | 0 |
| P2-15 | editable=true only | InsufficientPositiveEvidence deny | 0 | 0 |
| P2-16 | IsPassword=false only | InsufficientPositiveEvidence deny | 0 | 0 |
| P2-17 | allow predicate indeterminate | InsufficientPositiveEvidence deny | 0 | 0 |
| P2-18 | generation changes before issue | StaleContext deny | 0 | 0 |
| P2-19 | generation changes after issue/before claim | capability revoked | 0 usable | 0 |
| P2-20 | capability deadline passes | capability expired | 0 usable | 0 |
| P2-21 | capability claimed twice | second AlreadyConsumed | 1 first claim | 0 in Phase 2 |
| P2-22 | two concurrent claims | exactly one claim wins | 1 | 0 in Phase 2 |
| P2-23 | protected state toggles before issue | deny | 0 | 0 |
| P2-24 | product-owned synthetic ordinary field, all signals known | Allowed | 1 | 0 in Phase 2 |

## Permutation test

For cases with multiple simultaneous deny signals, shuffle signal ordering and assert the allow/deny truth value never changes. Canonical denial reason follows the stable priority table.
