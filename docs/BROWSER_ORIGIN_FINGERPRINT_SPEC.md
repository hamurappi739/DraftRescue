# Browser Origin Fingerprint Specification

**Status:** privacy-preserving identity design; acquisition mechanism remains experimental.

## 1. Goal

When browser recovery matching requires site identity, avoid persisting full URLs, paths, query strings, fragments, page titles, or form labels.

## 2. Preferred semantic input

Use an origin-level identity where safely obtainable:

```text
scheme + normalized host + effective port
```

Do not include path/query/fragment by default.

The exact source of browser origin is adapter-dependent and must not rely on reading arbitrary page text.

## 3. Transformation

Canonicalized origin string is transformed using the installation-keyed HMAC/fingerprint scheme from `IDENTITY_AND_FINGERPRINTING_SPEC.md`. Persist only versioned fingerprint token and necessary non-sensitive version metadata.

## 4. Private browsing gate first

Browser private-mode classification occurs before draft content read and before treating origin identity as permission. If the private detector cannot safely establish normal mode, deny.

## 5. Origin changes

A conflicting origin fingerprint is fatal for StrongMatch when the profile declares `browserOrigin` required/fatal. Same host with different relevant port/scheme is not silently merged unless normalization policy explicitly defines equivalence.

## 6. Redirect/navigation

Navigation/redirect does not prove completion. A changed origin creates new context evidence and may end active matching continuity, but previous eligible draft remains recoverable until completion/discard/expiry policy resolves it.

## 7. Tests

- https/http distinction where relevant;
- default/non-default port normalization;
- IDN/punycode canonicalization decision;
- host case normalization;
- path/query/fragment absent from persisted data;
- cross-origin navigation conflict;
- Incognito/InPrivate denies before content path.
