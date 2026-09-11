# App Profile Version and Compatibility Policy

**Status:** support governance baseline.

## 1. Why versioning matters

Target applications and browsers update independently. Accessibility trees, private-mode indicators, control patterns, process identities, and submit behavior can change without notice. "Worked once" is not permanent evidence of safety.

## 2. Two versions

Track separately:

- `profileVersion` — DraftRescue policy/adapter contract version;
- certified target version/range — external application versions actually tested.

The JSON manifest's `profileVersion` does not by itself certify all future app versions.

## 3. Compatibility states

For a given installed target version:

- `CertifiedSupported`
- `ExperimentalKnown`
- `UnknownVersion`
- `ExplicitlyBlocked`

Unknown target version must not automatically inherit security-sensitive support assumptions where private/secure detection could have changed.

## 4. Fail-closed upgrade policy

For browser profiles especially, a major/incompatible accessibility behavior change must degrade to unsupported/experimental until certification is repeated.

Exact version-range granularity is profile-specific; do not parse/version-match with naive lexicographic string comparison.

## 5. Existing drafts across profile updates

Stored draft metadata records preserve the profile ID/version used at capture. Recovery with a newer profile still requires fresh live target classification/matching; old profile permission is not inherited as restore authorization.

## 6. Profile rollback

Rollback may reduce support. It must not reinterpret stored fingerprints using incompatible normalization/version rules without explicit migration/version dispatch.

## 7. Certification artifact

Each `supported` profile release records:

- tested app/browser versions;
- Windows versions/builds;
- framework/control observations;
- secure/private negative tests;
- completion tests;
- recovery/restore capability;
- known limitations;
- date/build of certification.

## 8. Emergency incompatibility

MVP profile policy ships with application releases (ADR 0019). If an external update creates a security risk, the safe immediate behavior for unknown/incompatible conditions is fail-closed; a signed DraftRescue update can then adjust certification/profile rules.
