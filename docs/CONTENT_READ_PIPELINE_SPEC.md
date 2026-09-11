# Content Read Pipeline Specification

## Normative pipeline

```text
TargetFieldCandidateMetadata
  -> Phase 2 security evidence
  -> Allowed policy result
  -> capability issuance recheck
  -> AllowedFieldHandle (Fresh)
  -> atomic ClaimForRead()
  -> capability becomes Consumed
  -> binding/generation revalidation
  -> selected certified read strategy
  -> bounded provider read
  -> result validation
  -> FieldTextSnapshot
  -> generation check
  -> in-memory DraftTracker ApplySnapshot
```

There is no bypass edge from candidate metadata or policy result directly into a reader.

## Ordering requirements

1. Claim is atomic and occurs before provider content access.
2. A claimed capability is spent even if the provider throws, times out, returns too-large data, or target changes.
3. Binding/generation/profile compatibility is rechecked immediately before provider content access where the platform adapter can do so without another content read.
4. The read result carries the generation/sequence that authorized it.
5. Application rejects a result that is stale at apply time.

## Side-effect budget

A read attempt may access the certified target content and create one transient managed string/result. It may not log it, persist it, hash it for diagnostics, place it on clipboard, send it to UI, or attach it to an exception.
