# Recovery Matching Specification

**Status:** design contract for Phase 6; fingerprint algorithms remain implementation decisions.

## 1. Objective

Match a recoverable draft to the **same logical target** strongly enough that DraftRescue can decide whether direct Restore is safe.

A matching result is not merely “same application.” It is a multi-signal decision.

## 2. Separate two questions

1. **Should this draft be shown to the user?**
   - based on persisted recoverable state and retention.
2. **Can DraftRescue directly restore it right now?**
   - based on a fresh target match and security re-check.

A draft can remain Preview/Copy-capable even when direct Restore is unavailable.

## 3. Signal families

Potential signals from the canonical product direction:

- application executable identity;
- app profile identity/version family;
- top-level window identity/pattern;
- browser site/origin where safely available;
- accessibility-tree ancestry/role structure;
- automation/control type;
- stable automation identifiers when present;
- approximate field geometry;
- neighboring structural metadata;
- app-specific adapter signals.

No single signal is authoritative globally.

## 4. Stored context minimization

Persist only what later matching needs.

Prefer:

- normalized enums/categories;
- opaque hashes/fingerprints;
- coarse geometry buckets;
- origin-level browser identifiers rather than full URLs where possible;
- stable provider IDs only when they do not embed content.

Avoid persisting raw:

- page titles containing user data;
- full URLs with query strings/path parameters;
- field text/labels when not necessary;
- complete accessibility trees.

## 5. Match outcomes

Conceptual result:

```text
RecoveryMatchResult
- ExactOrStrongMatch
- PlausibleButInsufficient
- TargetUnavailable
- Mismatch
- Unsupported
- Unsafe
```

Only `ExactOrStrongMatch` may enable direct Restore, and even then a **fresh SecureInputGuard pass** is required.

`PlausibleButInsufficient` must not produce a “force restore” button in MVP.

## 6. Suggested evidence model

Do not expose or persist a magic universal percentage. Internally, matching may use weighted evidence, but acceptance is profile-aware.

Example categories:

### Required anchors

- same normalized application/profile;
- compatible control/editable role;
- current target passes security/privacy gating.

### Strong corroborators

- same stable field identifier;
- same site/origin + structural field path;
- same known compose surface in an app profile.

### Weak corroborators

- similar geometry;
- similar window-title pattern;
- same ordinal position in an accessibility subtree.

Weak corroborators may break ties but should not establish a restore target alone.

## 7. Freshness

Target matching must use current live metadata. Do not blindly restore based on handles/process IDs saved before an app restart; handles and runtime IDs can be reused or change.

## 8. Ambiguity

If two live fields match similarly:

- do not select one arbitrarily;
- return insufficient/ambiguous;
- keep Preview/Copy available;
- optionally bring the app to attention in a later UX iteration, but do not inject text.

## 9. Browser rules

For browser fields, matching should prefer origin/site and structural/editor identity over full URL equality. Full URL paths can change after navigation and may contain sensitive data.

Private browsing must be re-evaluated at restore time as well; a draft captured in a normal window must not be blindly restored into a newly opened private context.

## 10. Restore-time revalidation sequence

```text
User clicks Restore
    -> locate candidate current target
    -> normalize fresh context
    -> run SecureInputGuard again
    -> run profile-aware match
    -> verify target still stable
    -> perform supported restore mechanism
    -> verify operation result where possible
```

Any failure before mutation => no text injection.

## 11. Test scenarios

- same app, same field, same session -> restore allowed;
- same app, different field -> denied;
- same browser, different site -> denied;
- same site, two similar compose boxes -> ambiguous unless profile disambiguates;
- app restart with recreated correct field -> can match using durable signals, not old handles;
- target becomes password field after draft capture -> denied;
- private window appears with otherwise similar field -> denied;
- geometry changes due to resize/DPI -> geometry alone must not break a strong structural match;
- weak title match only -> insufficient;
- target disappears during restore -> no injection and draft remains recoverable.
