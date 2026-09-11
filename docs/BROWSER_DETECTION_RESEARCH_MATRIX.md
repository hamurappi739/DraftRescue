# Browser Detection Research Matrix

**Status:** experiment plan; no private-mode heuristic is considered accepted until measured.

## 1. Non-negotiable rule

Chrome Incognito, Edge InPrivate, Firefox Private, and analogous modes are denied by default. If DraftRescue cannot establish an approved non-private context with sufficient certainty, it does not read or persist field text.

## 2. Avoid fragile assumptions

Do not rely solely on:

- title text containing "Incognito" / "InPrivate";
- window color/theme;
- process count;
- keyboard shortcuts used to open a window;
- profile-directory guessing from unrelated processes.

These may be research signals but are not permission by themselves.

## 3. Required experiment matrix

For each supported browser, test:

| Scenario | Expected product outcome |
|---|---|
| normal window, ordinary page | may proceed to field security classification |
| private window | deny before text read |
| normal + private windows simultaneously | classify each target correctly |
| browser restart | no stale private/non-private classification |
| tab moved between windows | reclassify current target |
| profile switch | reclassify context |
| DevTools focused | unsupported/deny unless explicitly profiled |
| extension page | unsupported/deny unless explicitly profiled |
| browser chrome/address bar | deny/unsupported for MVP |
| password manager/credential UI | deny |
| payment/banking-like form | deny via field/content metadata policy before text read where possible |

## 4. Evidence record

Research adapter may collect metadata-only evidence such as:

```text
BrowserPrivacyEvidence
- BrowserFamily
- WindowIdentityToken
- ProviderSignals[]
- DetectorVersion
- Assessment: Normal / Private / Unknown
- AssessedAtUtc
```

No field text is permitted in this evidence.

## 5. Assessment rule

- `Private` -> `DeniedPrivateBrowsing`
- `Unknown` -> `DeniedUncertain`
- `Normal` -> continue to ordinary SecureInputGuard checks

`Normal` does not mean the field itself is safe.

## 6. Cache policy

Private-mode assessment may be cached only for a narrow window/context generation and short TTL. Any window recreation, target identity change, or conflicting signal invalidates cache.

Never cache "browser is safe" globally for the process because normal and private windows can coexist.

## 7. Version drift

Browser updates can alter accessibility trees. Profile versions record the validated browser-family strategy. A detected incompatible behavior should degrade to unsupported/uncertain until revalidated.

## 8. MVP support decision gate

A browser becomes `supported` only when automated/manual tests demonstrate:

1. private-mode denial before text read;
2. secure/password denial;
3. ordinary input/textarea capture with bounded overhead;
4. recovery matching that does not depend on full raw URL;
5. no capture in browser chrome/password-manager surfaces;
6. restore mechanism either validated or explicitly Copy-only.
