# Field Identity and Rebinding Specification

**Status:** canonical matching boundary for observation and recovery.

## 1. Problem

Accessibility elements are not durable identities. Browser/Electron/native frameworks may recreate controls while the user is still editing the same logical field. Conversely, superficially similar controls may be different sensitive or unrelated targets.

## 2. Identity layers

DraftRescue distinguishes:

- **EphemeralElementRef** — live UIA/platform object valid only briefly;
- **FieldFingerprint** — minimized keyed metadata used for correlation;
- **LogicalFieldIdentity** — profile-mediated interpretation for one draft lifecycle;
- **AllowedFieldHandle** — short-lived authorization/capability proving a specific live element passed current security checks.

An `EphemeralElementRef` is never persisted.

## 3. Rebinding during active editing

If the framework recreates the live element:

1. observe candidate metadata;
2. issue a new `ContextGeneration`;
3. run security/support classification again before content read;
4. profile decides whether new candidate can rebind to existing logical draft based on strong non-content evidence;
5. otherwise start separate/unknown lifecycle or deny.

Never rebind solely because text happens to match.

## 4. Fatal conflicts

Any of the following invalidates rebind unless a profile explicitly proves equivalent semantics without weakening security:

- application identity differs;
- security classification differs toward denied/uncertain;
- browser origin fingerprint conflicts where origin is required;
- field identity anchor conflicts;
- context generation was superseded.

## 5. Geometry

Approximate geometry is weak/optional evidence only. Window movement, DPI, layout, responsive web design, and scrolling make geometry unstable.

## 6. Accessibility names/labels

Raw names/labels are transient metadata. If useful for correlation they should be transformed under canonical fingerprinting/minimization rules and must not be persisted verbatim by default.

## 7. Recovery-time rebinding

Recovery requires stronger evidence than active-session continuity because the original live element no longer exists. Follow `RECOVERY_MATCHING_SPEC.md`; active-session rebinding success never becomes blanket authorization for later Restore.

## 8. Tests

- same control object, repeated events;
- control recreated with stable profile anchors;
- two visually identical fields;
- browser same path different origin;
- window moved/resized/DPI change;
- stale element after navigation;
- secure replacement control appears where ordinary editor existed;
- field reorder/list virtualization.
