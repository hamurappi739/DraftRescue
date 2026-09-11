# Security Classifier Decision Tree v1

## Principle

The classifier grants access only by positive proof under a certified target profile. It is not a blacklist of bad fields.

## Inputs

- normalized `TargetFieldCandidateMetadata`;
- resolved `AppProfile` and certified target version;
- private-mode result when applicable;
- target-class-specific structural signals;
- current integrity/provider health;
- current `ContextGeneration`.

No draft text is an input.

## Decision order

```text
1. own process / unsupported desktop / integrity boundary?
   yes -> Deny(UnsupportedContext)

2. profile missing / invalid / ambiguous / incompatible version?
   yes -> Deny(UnsupportedProfile)

3. browser/private context applicable?
   private -> Deny(PrivateBrowsing)
   unknown when profile requires proof -> Deny(UncertainPrivateMode)

4. IsPassword == true?
   yes -> Deny(Password)

5. required password signal unavailable/unknown?
   yes -> Deny(UncertainSecureState)

6. target class inherently sensitive or structurally matched to credential/PIN/payment/banking rule?
   yes -> Deny(SensitivePurpose)

7. provider unhealthy/stale/context generation changed?
   yes -> Deny(StaleOrUnavailable)

8. profile's positive allow predicate satisfied?
   no -> Deny(InsufficientEvidence)

9. create short-lived AllowedFieldHandle bound to generation + target token + profile version
```

## Precedence

Deny evidence dominates allow evidence. Evaluation order must not permit an early positive rule to bypass a later hard deny; implementation should model hard denies separately and test permutation invariance.

## Generic target policy

The generic classifier is intentionally narrow. A generic unknown editable field is **not** Allowed merely because it is editable and `IsPassword=false`.

For MVP:

- known-safe synthetic fields can be allowed in test mode;
- a specifically certified Notepad document surface may be allowed by its profile;
- browser web-form surfaces remain denied until browser/private-mode and sensitive-purpose classification are separately certified in Phase 7;
- address bars, search boxes, browser chrome, credential dialogs, payment flows, and unknown custom editors are unsupported.

## Capability lifetime

An `AllowedFieldHandle` is a one-read capture capability. It is consumed when a read claim begins and cannot be reused, even if the subsequent provider read fails. It is also invalidated before claim by:

- `ContextGeneration` change;
- target process/binding exit or replacement;
- profile/version change;
- fresh hard-deny evidence;
- monotonic deadline expiry (1000 ms default, 2000 ms hard cap).

It is never persisted/serialized/logged and can never authorize Restore. Every future content read requires a new evaluation and capability.

## Reason codes

Classifier result should carry structural reason enums, not raw labels or text. Reasons are safe for metrics/logging only after explicit audit.
