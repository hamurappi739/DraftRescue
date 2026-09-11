# FieldTextSnapshot Canonical Model

`FieldTextSnapshot` is a transient Application value representing one complete current text state observed from one allowed target field.

## Required fields

```text
ContextGeneration
SnapshotSequence
CaptureAttemptId          // random/opaque, process-local diagnostic correlation; not content-derived
ProfileId
ReadStrategy              // TextPatternDocument | ValuePatternCertified
CapturedAtMonotonic
CapturedAtUtc             // operational timestamp only
Text                      // transient plaintext
TextLengthUtf16
IsEmpty
```

## Forbidden fields

No URL, title, contact name, document filename, field label, raw AutomationElement, RuntimeId, HWND serialization, formatting runs, selection/caret history, or keystroke history.

## Invariants

- `TextLengthUtf16 == Text.Length`.
- `IsEmpty == (Text.Length == 0)`.
- one snapshot represents full current state, not delta/key history;
- snapshot cannot be serialized by generic JSON persistence APIs;
- snapshot cannot cross process boundaries;
- stale generation never mutates tracker state.

## Ownership

The snapshot exists only long enough for Application to compare/apply current draft state. Durable persistence is a later phase and must receive a protected payload, not this type directly.
