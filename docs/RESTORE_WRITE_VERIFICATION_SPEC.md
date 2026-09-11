# Restore Write Verification Specification

**Status:** mandatory design for direct Restore.

## 1. Purpose

A write API returning success is not enough to tell the user that recovery succeeded. DraftRescue needs a target-aware verification result that does not leak plaintext into logs or diagnostics.

## 2. Result model

Conceptual result:

```text
RestoreWriteResult
- Verified
- AppliedButUnverified
- RejectedBeforeWrite
- WriteFailed
- TargetChanged
- SecurityStateChanged
- VerificationMismatch
```

Only `Verified` is presented as unqualified success. `AppliedButUnverified` uses conservative copy such as "DraftRescue sent the draft to the field, but couldn't verify the result."

## 3. Pre-write snapshot

Immediately before mutation, capture only the minimum transient information needed for verification:

- fresh allowed target capability;
- context generation / field identity;
- expected text length or keyed transient digest where useful;
- current target state if the adapter can read it safely.

Do not persist verification plaintext/digests after the operation.

## 4. Verification hierarchy

Preferred order:

1. adapter/API returns the resulting value and identity can be rechecked;
2. safe re-read of the same allowed target and exact in-memory comparison;
3. safe length + keyed transient digest comparison where exact value cannot be retained beyond the call;
4. no safe verification available -> `AppliedButUnverified`.

A weak visual/UI state change is not enough to claim exact text restoration.

## 5. Comparison rules

Verification must use adapter-defined normalization only when that adapter proves the target transforms text predictably. Do not globally trim whitespace, normalize line endings, alter Unicode, or collapse spaces when deciding equality.

For a target that canonicalizes line endings, the adapter may define a reversible/known normalization contract with tests.

## 6. Non-empty target

If target content is unrelated and non-empty, fail before write. Do not verify after destructive overwrite as a way to discover that matching was wrong.

## 7. Race handling

Revalidate field identity and security state both:

- immediately before write;
- immediately before/while verifying if the API requires a second read.

If the target changed, do not perform compensating writes into a possibly different field.

## 8. Post-success cleanup

For `Verified`:

1. mark/delete the recoverable record atomically;
2. clear transient plaintext buffers/references as practical;
3. refresh recovery UI;
4. suppress duplicate repeated-click actions.

For `AppliedButUnverified`:

- do not automatically delete the only recoverable copy unless the profile has a separately proven idempotent policy;
- UI must avoid encouraging repeated Restore that could duplicate text;
- offer Preview/Copy and a deliberate Discard after the user confirms.

## 9. Failure policy

Never auto-fallback to clipboard or synthetic typing. A failed direct write leaves the draft recoverable and reports a structural reason without content.

## 10. Tests

- exact verified write;
- API says success but re-read mismatches;
- write succeeds then target changes before verification;
- target changes before write;
- security state changes before write;
- target normalizes CRLF/LF under certified adapter;
- unrelated non-empty content;
- double-click/reentrancy;
- verification exception contains no text in logs;
- `AppliedButUnverified` does not silently discard sole recoverable copy.
