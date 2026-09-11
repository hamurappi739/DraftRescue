# Security Policy Evaluation Specification

## Design

Split security classification into two stages:

1. **Signal collection** — platform/profile adapters gather approved structural metadata.
2. **Pure policy evaluation** — deterministic function maps `SecurityEvidenceSet` to `SecurityDecision`.

The policy evaluator performs no I/O and has no Windows/UIA dependency.

## Result model

```text
SecurityDecision
- Allowed(AllowBasis)
- Denied(CaptureDenialReason, StableErrorCode)
```

An `Allowed` decision is not itself permission to read text. Only the capability issuer may convert it into an `AllowedFieldHandle` after rechecking binding/generation.

## Deterministic evaluation order

Conceptually:

```text
A. self/unsupported/integrity gate
B. profile resolution/version gate
C. private-mode gate where applicable
D. protected-content gate
E. sensitive-purpose gate
F. provider health / binding freshness gate
G. structural positive-allow gate
H. issue Allowed decision
```

Implementation may evaluate hard denies in any internal order only if `C-019` permutation tests prove the same result class. For diagnostics, when multiple hard denies are present, choose the canonical reason by the above priority.

## Canonical reason priority

1. `UnsupportedContext`
2. `UnsupportedProfile`
3. `PrivateBrowsing`
4. `UncertainPrivateMode`
5. `SecureField`
6. `UncertainSecureState`
7. `CredentialLikeField`
8. `BankingOrFinancialField`
9. `StaleContext`
10. `ProviderUnavailable`
11. `InsufficientPositiveEvidence`

This priority exists only for stable diagnostics. It must not affect the deny/allow truth value.

## No text-based classification

The evaluator has no string input representing typed target content. Any API that adds raw draft text to `SecurityEvidenceSet` violates `P-002`, `P-005`, and `P-031`.

## Re-evaluation

Classification is performed for each content-read attempt. Previous `Allowed` results are not reused as ambient field/process trust.

## Property acquisition failure

If a profile marks a security signal as required and acquisition yields `Unknown`, `Unavailable`, `Failed`, timeout, stale element, or access denied, the evaluator denies.

## Pure-policy test properties

Required table/property tests:

- hard deny beats allow;
- permutation of evidence collection order cannot produce Allow;
- every required unknown state denies;
- `IsPassword=false` alone cannot allow;
- `Editable=true` alone cannot allow;
- only exactly satisfied certified predicate can allow;
- browser private-mode unknown denies when browser profile requires proof;
- incompatible app version denies before allow predicate is considered.
