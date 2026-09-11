# App Profile Resolver Algorithm

**Status:** accepted deterministic resolver baseline.

## 1. Goal

Resolve one live application identity to exactly one eligible app profile, or fail closed. Runtime must never pick an arbitrary profile merely because multiple manifests look plausible.

## 2. Inputs

```text
ForegroundApplicationIdentity
- ExecutableName
- PackageFamilyName?
- PublisherIdentity?
- Product/FileVersion?
- ProcessIntegrityClass
- BrowserFamilyHint?
```

Only structural application identity belongs here. No field text, page title, URL, or accessibility name.

## 3. Resolution stages

### Stage A — hard platform exclusions

Reject/unsupported when:

- target is DraftRescue itself;
- process identity cannot be established safely;
- integrity/session boundary is unsupported;
- identity result is stale for the current `ContextGeneration`.

### Stage B — candidate manifest selection

Select manifests whose declared application identity rules match.

Specific identity forms outrank generic forms conceptually, but overlapping `supported` manifests at equal/similar specificity are a **configuration error**, not a runtime priority contest.

### Stage C — validate manifest

Schema-invalid manifest -> unusable. Never partially interpret it.

### Stage D — support level

- `unsupported` -> explicit deny;
- `experimental` -> available only in explicit developer/test opt-in mode;
- `supported` -> continue.

Production MVP does not silently enable experimental profiles.

### Stage E — target version compatibility

For `supported`, the installed target must be inside a certified compatibility declaration. If version cannot be obtained or parsed according to the profile's declared scheme, outcome is `UnknownVersion`, not supported.

Explicit blocked range wins over certified range.

### Stage F — ambiguity

If more than one eligible production profile survives resolution, return `AmbiguousProfiles` and deny security-sensitive capture/restore. Do not use manifest file order, dictionary order, first-match, or arbitrary numeric priority to hide the conflict.

### Stage G — result

```text
AppProfileResolution
- Supported(profile)
- Experimental(profile)
- UnsupportedNoProfile
- UnsupportedVersion
- ExplicitlyBlocked
- AmbiguousProfiles
- InvalidProfile
```

## 4. Generic profiles

A future generic profile is allowed only if it is explicitly narrow (for example a tested standard Windows edit control family) and has its own security certification. It is not a fallback that says “unknown app => try UIA anyway.”

## 5. Build-time overlap validation

Before release, profile tests should generate representative application identities and assert that no two `supported` profiles claim the same identity/version combination unless an explicit composition design exists and has its own ADR.

## 6. Profile updates

Persisted drafts retain capture-time profile ID/version metadata. A newer resolver does not retroactively authorize Restore. Restore performs resolution against the **current live target** and then uses compatibility-aware matching/migration rules.

## 7. Browser rule

Browser family alone is insufficient identity. Browser profiles still require executable/package identity and a certified target-version rule. Private-mode detection happens after profile resolution but before content read.
