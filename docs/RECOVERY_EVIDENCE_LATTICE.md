# Recovery Evidence Lattice

**Status:** accepted replacement for a universal numeric confidence score.

## 1. Principle

DraftRescue does not compute a global “82% match” and compare it with a magic threshold. Restore eligibility is a deterministic evidence predicate defined by profile class.

The public result remains categorical:

```text
NoMatch
Ambiguous
StrongMatch
```

## 2. Evidence classes

### Mandatory gates

All must pass before matching can be Strong:

- fresh current target exists;
- current target is editable and supported;
- fresh security/private classification is `Allowed`;
- normalized application identity matches the draft's allowed profile family;
- current target profile/version is compatible.

Failure -> `NoMatch` or unsafe/unsupported result.

### Identity anchors

Examples:

- stable app-specific field token;
- stable automation/control identifier fingerprint;
- certified structural editor path fingerprint;
- browser-origin + structural field identity pair.

At least one profile-approved identity anchor is normally required.

### Context corroborators

Examples:

- window-context fingerprint;
- browser-origin fingerprint;
- app-specific compose-surface token;
- coarse geometry bucket;
- compatible ancestry/role fingerprint.

These strengthen identity but weak values cannot independently authorize mutation.

### Fatal conflicts

Any profile-declared fatal conflict immediately prevents StrongMatch:

- different application/profile family;
- private/secure state conflict;
- browser-origin conflict where origin is required;
- known different field identity;
- current target read-only/non-editable;
- incompatible target version.

## 3. Deterministic evaluation order

```text
if mandatory gate fails:
    NoMatch/Unsafe
else if any fatal conflict:
    NoMatch
else evaluate profile strong-match predicate
    if exactly one live target satisfies it:
        StrongMatch
    if more than one satisfies it:
        Ambiguous
    else:
        NoMatch or Ambiguous according to missing-vs-conflicting evidence
```

No tie-breaking by screen order or “closest geometry”.

## 4. Baseline profile predicates

### Native/generic standard editor

Strong requires:

```text
application identity
AND compatible control/editor role
AND one stable field-identity anchor
AND (window-context corroborator OR app-specific corroborator)
AND no fatal conflict
```

Geometry may corroborate but never substitute for the stable field identity.

### Browser editor

Strong requires:

```text
browser application/profile identity
AND certified normal (non-private) current window
AND required origin fingerprint when profile declares it
AND structural field/editor identity
AND no fatal conflict
```

Full URL equality is not required or persisted.

### App-specific adapter

The profile owns a named predicate whose required signals are enumerated in the profile/certification fixture. The adapter cannot bypass global gates.

## 5. Missing vs conflicting evidence

- Missing weak corroborator -> may still StrongMatch if profile predicate remains satisfied.
- Missing required anchor -> not StrongMatch.
- Conflicting required/fatal signal -> `NoMatch`, not merely lower confidence.
- Two equally valid targets -> `Ambiguous`.

## 6. Persistence

Persist evidence tokens/versions needed for future comparison, not a numeric confidence result. A previous `StrongMatch` is never stored as future authorization.

## 7. Testing

Every supported profile must publish table-driven fixtures:

```text
PersistedEvidence + LiveEvidence -> ExpectedMatchKind
```

Tests must include single-signal matches, weak-only matches, fatal conflicts, duplicate candidate targets, version drift, resize/DPI changes, and recreated accessibility elements.
