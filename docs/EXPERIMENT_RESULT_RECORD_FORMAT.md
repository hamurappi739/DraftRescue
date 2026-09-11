# Experiment Result Record Format

## Purpose

Standardize Phase 1 research evidence without recording user content.

## Record

```text
ExperimentResult
- SchemaVersion
- ExperimentId
- RunId
- StartedAtUtc
- DurationMs
- HostOsBuild
- DraftRescueBuildId
- TargetApplicationIdentity
- TargetVersion
- ScenarioId
- Outcome : Pass | Fail | Inconclusive
- Measurements
- StructuralObservations
- FailureCodes[]
- InvariantIds[]
- TestIds[]
- ContentSafetyAudit
```

## ContentSafetyAudit

Required fields:

```text
RawTargetTextCaptured      : false
RawDynamicUiaStringsLogged : false
ClipboardRead              : false
ClipboardWritten           : false
NetworkTextSent            : false
TestDataClass              : Synthetic | MetadataOnly
```

Any `true` value in a forbidden field invalidates the experiment and requires investigation before evidence is used.

## Structural observations

May contain enums, booleans, counts, durations, bounded numeric identifiers, version numbers, and audited fingerprints. It must not contain raw field text, document title, URL, accessibility Name, HelpText, label text, or clipboard text.

## Human notes

Human-authored notes follow the same content rules. Describe topology semantically (`document surface`, `find box`, `dialog`) rather than pasting UI text.

## Machine schema

See `../specs/experiment-result.schema.json`.
