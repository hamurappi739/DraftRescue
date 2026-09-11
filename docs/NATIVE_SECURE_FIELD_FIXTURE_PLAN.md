# Native Secure-Field Fixture Plan

## Purpose

Create a controlled Windows-only test application for Phase 2 privacy testing. It uses synthetic content only and is never pointed at real credentials or user applications.

## Fixture surfaces

The test harness should expose clearly separated controls:

1. ordinary single-line editable field;
2. ordinary multi-line editable field;
3. read-only field;
4. native password/protected field;
5. synthetic username/credential-purpose field;
6. synthetic PIN/OTP-purpose field;
7. synthetic payment/card-purpose field;
8. field with delayed security metadata response;
9. field replaced while classification is in flight;
10. field whose protected state toggles before capability claim;
11. two ordinary fields for context-generation switching;
12. disabled/offscreen surface for unsupported-state tests.

Use canaries such as `DRAFTRESCUE_SYNTHETIC_CANARY_42` only.

## Native password proof

The fixture must verify, through the same UI Automation metadata surface DraftRescue will consume, that the protected control reports the expected protected-content signal. Microsoft documents `AutomationElementInformation.IsPassword` as indicating whether the element contains protected content.

The test does not read password content to prove denial.

## Instrumentation

Harness-side spies/counters:

```text
MetadataRequests
ProtectedSignalRequests
CapabilityIssueAttempts
CapabilityIssueSuccesses
ContentReadInvocations      MUST remain 0 in Phase 2
LogCanaryOccurrences        MUST remain 0
```

## Fault controls

Developer-only controls may:

- block a metadata response behind a barrier;
- destroy/recreate the target control;
- switch focus/generation;
- toggle ordinary -> protected;
- simulate profile version mismatch;
- simulate access denied/provider failure through adapters.

## Evidence artifact

Each run outputs only schema-valid structural evidence: booleans, enums, durations, counts, product-owned fixture IDs. No raw field content or UIA tree dump.
