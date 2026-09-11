# Restore Safety Specification

**Status:** mandatory contract for Phase 6.

## 1. Principle

Restore is a write operation into another application's UI. It must be treated as a security-sensitive mutation, not a convenience command.

Default rule: **no blind injection**.

## 2. Restore preconditions

All must be true:

1. draft exists and is not expired;
2. draft payload decrypts successfully;
3. target application/field is found;
4. target passes a fresh secure/private classification;
5. recovery matcher returns a strong profile-appropriate match;
6. target remains stable immediately before mutation;
7. selected restore mechanism is supported for that target.

If any precondition fails, direct Restore is unavailable/aborted without modifying the target.

## 3. Mechanism preference

Preferred order for future implementation:

1. **native/accessibility value-setting mechanism** supported by the target and proven safe;
2. app-specific supported adapter;
3. explicit clipboard-assisted/user-driven fallback only after its own security design;
4. synthetic keystroke injection is not a default fallback and requires a separate ADR/security review.

Microsoft UI Automation exposes different patterns depending on control type; some text surfaces are readable through TextPattern while modification may require ValuePattern or another supported adapter. This variability is why restore must be capability-driven per profile.

## 4. Existing text in target

Do not silently overwrite unrelated non-empty content.

Profile must define one of:

- target is expected empty after recreation and restore may populate it;
- target contains a known prefix/current draft and safe replacement semantics are proven;
- direct restore is disabled and user uses Preview/Copy.

MVP should prefer conservative behavior over merging two arbitrary text versions.

## 5. No automatic restore

DraftRescue must not automatically inject text merely because a matching window appears. User explicitly triggers Restore.

## 6. Race handling

Between match and mutation the field can change.

Implementation should use a short critical flow and re-check the field identity immediately before the write. If identity changed, abort.

## 7. Result verification

Detailed outcome semantics are canonicalized in `RESTORE_WRITE_VERIFICATION_SPEC.md`.


Where the target API allows safe verification, confirm that the intended value was applied.

Do not log the expected or actual text during verification.

If verification is impossible, return an `Unverified`/conservative result rather than claiming success.

## 8. After success

On confirmed success:

- remove the recoverable persisted payload promptly;
- refresh UI;
- allow future observation to create a new active lifecycle only for subsequent edits.

On failure:

- keep the draft recoverable unless corruption/expiry requires deletion;
- show a safe user-facing error;
- never retry repeatedly without user intent.

## 9. Clipboard

Copy is explicitly requested and inherently exposes plaintext to the OS clipboard. Therefore:

- never copy automatically after restore failure;
- never copy when opening Preview;
- show success feedback without logging content;
- clipboard auto-clear is not used in MVP; DraftRescue does not later destroy user-owned clipboard data automatically (ADR 0015).

## 10. Unsupported cases

Direct Restore should be unavailable when:

- only weak match evidence exists;
- the app requires synthetic typing and that mechanism has not been approved;
- the editor cannot be set programmatically in a tested way;
- elevation/integrity boundaries prevent safe interaction;
- the target is now secure/private/unsupported.

## 11. Required tests

- restore writes only to the matched field;
- field switch during operation aborts;
- password/private target aborts;
- non-empty unrelated target does not get overwritten;
- restore failure keeps draft available;
- success removes recoverable payload;
- repeated click while restoring does not duplicate content;
- no clipboard mutation unless Copy/approved clipboard flow is explicitly used;
- no raw text in exception/log path.
