# Target Support Level State Machine

**Status:** canonical governance model.

## States

```text
Unsupported
   |
   | targeted research begins
   v
Experimental
   |
   | certification gates pass
   v
Supported
   |
   | incompatible target update / security regression
   v
Suspended/Unsupported
```

`Suspended` may be represented operationally as unsupported plus a reason code; a separate public enum is optional.

## Unsupported

No normal capture. May have explicit developer fixtures/research tooling outside production behavior.

## Experimental

Only controlled/internal/explicit opt-in testing. Never presented to ordinary users as guaranteed protection.

## Supported

Exact profile/version range passed certification and privacy negative tests.

## Regression

Any critical secure/private false-negative or wrong-target Restore defect immediately invalidates normal support for the affected range until fixed and recertified.

## Promotion checklist

Promotion is evidence-based, not a JSON edit:

1. field discovery stable;
2. classify-before-read proven;
3. secure/credential/private negative matrix green;
4. snapshot correctness green;
5. completion semantics documented;
6. recovery matching green;
7. restore either certified or explicitly Copy-only;
8. performance/soak acceptable;
9. known limitations documented;
10. release privacy checklist green for target.
