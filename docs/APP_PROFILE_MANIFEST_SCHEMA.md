# App Profile Manifest Schema

**Status:** accepted format direction. JSON Schema lives at `../specs/app-profile.schema.json`.

## 1. Purpose

App profiles describe support policy and adapter selection. They do not contain executable code and cannot weaken global security invariants.

## 2. Precedence

Global privacy rules always win over app profile rules.

A profile cannot:

- allow `IsPassword=true`;
- allow a field classified as credential/banking/private;
- bypass fresh Restore revalidation;
- enable plaintext logging;
- enable global keyboard capture.

## 3. Manifest fields

```text
schemaVersion
profileId
profileVersion
displayName
supportLevel
applicationMatch
frameworkHints
capturePolicy
browserPolicy?
completionPolicy
recoveryPolicy
restorePolicy
targetCompatibility?
knownLimitations
```

## 4. Support levels

- `unsupported` — explicit deny.
- `experimental` — opt-in/developer testing only.
- `supported` — eligible for normal capture subject to global policy.

Unknown app = unsupported by default during early MVP unless a generic profile is explicitly enabled for a narrow control family.

## 5. Application matching

Application match may use:

- normalized executable names;
- signed publisher identity in a later version;
- package family identity where applicable;
- browser family marker.

Do not match solely on window title.

## 6. Capture policy

Declares capability strategy, not permission to bypass security:

```text
capturePolicy:
- genericUiaValue
- genericUiaText
- appSpecificAdapter
- disabled
```

Reader selection happens only after SecureInputGuard returns Allowed.

## 7. Browser policy

For browser profiles:

```text
browserPolicy
- family
- privateModeHandling = deny
- privateModeDetectorId
- allowedOriginFingerprinting
```

If private-mode detector cannot establish a safe non-private state when the policy requires it, result is `DeniedUncertain` or `DeniedPrivateBrowsing`, never Allowed.

## 8. Completion policy

Profiles may describe strong completion evidence such as a known action/field transition, but focus loss alone can never be declared completion.

## 9. Recovery policy

Profiles declare required evidence classes for `StrongMatch`. They do not supply a numeric confidence threshold directly to UI.

Example concept:

```text
requiredStrongAnchors = [application, fieldIdentity]
optionalAnchors = [windowContext, browserOrigin, geometry]
conflictIsFatal = [browserOrigin, securityState]
```

## 10. Restore policy

Allowed mechanisms are capability declarations, e.g.:

- `valuePattern`
- `legacyAccessibleValue` (only after explicit validation)
- `appSpecificAdapter`
- `copyOnly`

No `sendKeys` generic fallback in MVP unless separately approved by ADR due to wrong-target risk.


## 11. Target compatibility

A `supported` profile must declare certified target-version compatibility. Conceptual fields:

```text
targetCompatibility
- versionScheme: windowsFileVersion | semver | chromiumMajor | opaqueExact
- certifiedRanges[]
- blockedRanges[]?
```

Explicitly blocked wins. Unknown/unparseable version fails closed. `experimental` profiles may omit a certified range while research is in progress.

Runtime resolution follows `PROFILE_RESOLVER_ALGORITHM.md`; overlapping production profiles are not resolved by file order.

## 12. Versioning

Any change that affects matching/normalization/security behavior increments `profileVersion`. Existing draft metadata records preserve the profile version used when created.

## 13. Validation

Profiles are schema-validated at build/test time. Invalid manifests are not partially interpreted at runtime.
