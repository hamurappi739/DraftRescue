# Allowed Capability Registry Specification

## Responsibility

`Platform.Windows` owns a process-local registry that maps an opaque ephemeral `CandidateBindingId` to the live platform target needed by the future eligible reader. Application/Desktop never receive raw `AutomationElement` or COM objects.

## Entry

```text
BindingEntry
- CandidateBindingId (unguessable/random or process-local opaque id)
- ContextGeneration
- ProcessId
- ProfileId + ProfileRevision
- CreatedAtMonotonic
- LastValidatedAtMonotonic
- State = Current | Revoked
- platform-private live element reference
```

No target text, title, label, URL, or persisted identity lives in the registry.

## Lifecycle

- create during metadata reconciliation;
- replace/revoke when the logical candidate changes;
- capability issuance references an existing Current entry;
- claim verifies entry/generation/profile/deadline;
- context change/process exit revokes affected entries;
- consumed capability does not create durable history;
- remove dead entries eagerly and sweep bounded stale entries periodically.

## Bounds

MVP target: one current binding plus a very small bounded number of in-flight/revoking bindings. The registry must never become a chronological focus history.

Implementation must expose diagnostics as counts only (`current_binding_count`, `revoked_pending_cleanup_count`), not identities.
