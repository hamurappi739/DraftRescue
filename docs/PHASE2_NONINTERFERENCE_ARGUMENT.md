# Phase 2 Non-Interference Argument

## Safety claim

For every Phase 2 execution path whose final security result is not `Allowed`, the number of target-content reads is exactly zero.

Formally, at the architectural level:

```text
Decision != Allowed  =>  ContentReadInvocationCount == 0
```

And during Phase 2 as a whole:

```text
ProductionContentReaderImplementationCount == 0
```

## Why the claim should hold

### 1. Observation has no content

Phase 1 produces only `TargetFieldCandidateMetadata`.

### 2. Classifier has no content-reader dependency

Signal collectors and policy evaluator consume metadata only. Their DI graph excludes `IEligibleFieldTextReader`.

### 3. Capability is required

Future Phase 3 content reader accepts only `AllowedFieldHandle`, not arbitrary candidate metadata/UIA element handles.

### 4. Capability issuer rejects all non-Allow outcomes

No API converts a deny/uncertain result to a capability.

### 5. Capability is one-read, generation-bound and short-lived

A previous allow cannot become ambient trust.

### 6. No permissive fallback

Timeout/stale/unknown/unsupported states return deny and create no capability.

## Attack-path review

The following must be structurally impossible or test-failing:

```text
Denied -> reader
Exception -> fallback reader
Unknown IsPassword -> reader
Unknown profile -> reader
Stale element -> reader
Browser private unknown -> reader
Capability expired -> reader
Capability reused -> reader
Capability from generation N -> read generation N+1
UI/ViewModel -> arbitrary reader
Repository -> arbitrary reader
```

## Proof obligations for implementation

Architecture tests must inspect:

- constructor dependencies of security handlers;
- project references;
- registered services in Phase 2 composition;
- method signatures for reader/capability boundary;
- absence of forbidden UIA content APIs in Phase 1/2 source paths;
- negative fixture spy counts.

## Limits

This is an engineering non-interference argument, not a formal mathematical proof of Windows/UIA providers. It proves DraftRescue's own architecture does not intentionally cross the content boundary before authorization. Provider bugs outside the product boundary remain handled by fail-closed behavior and certification tests.
