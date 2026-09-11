# Candidate Metadata Normalization Specification

## Purpose

Turn platform-specific focus/foreground observations into a small, deterministic, content-minimized application contract.

## Output type

```text
TargetFieldCandidateMetadata
- ContextGeneration       : ulong
- ObservationSequence     : ulong
- Application             : ApplicationIdentity
- TopLevelWindowHandle?   : opaque platform token
- ElementRuntimeToken?    : opaque process-local token
- ControlKind             : normalized enum
- FrameworkKind           : normalized enum
- IsPassword              : True | False | Unknown
- IsEnabled               : True | False | Unknown
- IsKeyboardFocusable     : True | False | Unknown
- HasKeyboardFocus        : True | False | Unknown
- IsOffscreen             : True | False | Unknown
- GeometryBucket?         : coarse normalized rectangle bucket
- AutomationIdFingerprint?: ephemeral/keyed fingerprint when policy permits
- ClassNameToken?         : normalized bounded token when policy permits
- CapabilityHints         : bit flags only
- IntegrityCompatibility  : Compatible | Incompatible | Unknown
- ProviderHealth          : Healthy | Degraded | Failed | Unknown
```

No content-bearing field exists in this contract.

## Normalization rules

### Strings

Only explicitly allowlisted structural strings may cross from platform into normalized metadata.

- trim ASCII/control whitespace;
- reject NUL/control characters;
- cap UTF-16 length before allocation/copy where possible;
- map known framework/class identifiers to enums/tokens;
- do not preserve unknown free-form strings by default;
- do not persist raw structural strings unless a separate spec allows it.

### Booleans

Provider exception or unsupported property becomes `Unknown`, not `False`.

### Geometry

Exact rectangles are transient. For fingerprints/correlation, prefer a coarse relative bucket and only after the top-level client area is known. Geometry is corroborating evidence, never sole identity.

### Runtime identifiers

UIA runtime IDs and `AutomationElement` object identity are ephemeral. They may help within one process lifetime/context generation but are not durable field identity across app restart/update.

### Process/application identity

Resolve executable identity outside UIA dynamic labels. Canonical application identity should derive from process image/package identity/version through the Windows platform layer, subject to access constraints.

## Determinism

Equivalent raw metadata must normalize to byte-for-byte equivalent semantic values independent of event ordering. Normalization is pure logic and should be exhaustively table-tested.

## Bounds

The normalized object must have a hard maximum serialized diagnostic size even though it is not persisted. Any future debug representation must redact/hash optional structural tokens and never include raw dynamic strings.

## Failure behavior

If a required structural property is unavailable:

- represent it as Unknown/missing;
- do not invent a default;
- let profile/security classification fail closed if that property is required.
