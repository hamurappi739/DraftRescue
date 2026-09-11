# App Support Certification Template

Create one completed certification record per app + material version/profile before `supportLevel=supported`.

## Identity

- Application:
- Application version:
- Windows version/build:
- Profile ID/version:
- Framework/provider observations:
- Test date:

## Observation

- Foreground detection reliable: Pass/Fail
- Focused field localization reliable: Pass/Fail
- Own-process exclusion: Pass/Fail
- Event rate/resource budget: Pass/Fail
- Known stale-element cases:

## Security

- Password/protected field denied before text read: Pass/Fail
- Credential-like fixtures denied: Pass/Fail
- Banking/payment-like fixtures denied: Pass/Fail
- Unknown/error timeout fails closed: Pass/Fail
- Private mode (if browser) denied before text read: Pass/Fail/N/A

## Read capability

- Supported field families:
- Reader mechanism(s):
- Multi-line behavior:
- Unicode/IME behavior:
- Maximum tested size:
- Known unsupported editor types:

## Draft semantics

- Focus loss preserves draft: Pass/Fail
- Stable clear handling: Pass/Fail
- Strong completion evidence validated: Pass/Fail/Not implemented
- Crash/close recoverability: Pass/Fail

## Recovery matching

- Required strong anchors:
- Ambiguous fixture results in no Restore: Pass/Fail
- Reopened same context strong-match: Pass/Fail
- Wrong window/field rejected: Pass/Fail

## Restore

- Restore mechanism:
- Fresh security recheck: Pass/Fail
- Target-change race abort: Pass/Fail
- Unsupported target => Copy-only: Pass/Fail
- Verification after write:

## Privacy canaries

- Logs clean: Pass/Fail
- DB raw scan clean: Pass/Fail
- No unexpected network path: Pass/Fail
- Private context clean: Pass/Fail/N/A

## Decision

- `unsupported` / `experimental` / `supported`
- Known limitations shown to product/support:
- Required follow-up:
- Reviewer:


## Version invalidation

If the installed target falls outside the certified range or a framework/security-relevant behavior changes, follow `PROFILE_VERSION_COMPATIBILITY_POLICY.md`; do not assume certification carries forward.
