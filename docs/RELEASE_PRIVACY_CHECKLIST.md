# Release Privacy Checklist

Run before any public build, not only before 1.0.

## Architecture

- [ ] No global keylogger/history code introduced.
- [ ] Platform, persistence, UI boundaries still match architecture tests.
- [ ] No new generic plaintext repository/logging API.
- [ ] Accepted privacy ADRs reviewed for changes.

## Capture

- [ ] Password/protected fixtures never reach text reader.
- [ ] Credential/banking fixtures denied.
- [ ] Unknown/provider failures deny.
- [ ] Browser private mode tests pass for every browser profile marked supported.

## Persistence

- [ ] Known plaintext canary absent from raw database/log/settings files.
- [ ] One logical draft does not accumulate revision rows.
- [ ] DPAPI/protection failure has no plaintext fallback.
- [ ] Expiry is enforced logically and physically cleaned.
- [ ] Discard-all works without decryption.

## Recovery

- [ ] Restore performs fresh security check.
- [ ] Ambiguous match has no force-restore route.
- [ ] Target-change races abort safely.
- [ ] Unsupported write path degrades to Copy-only.
- [ ] Restore verification mismatch is not reported as verified success.

## UI

- [ ] No history/activity wording or hidden history screen.
- [ ] List does not bulk-decrypt drafts.
- [ ] Copy is explicit.
- [ ] Privacy statements match actual behavior.

## Diagnostics/network

- [ ] Canary absent from logs/crash output.
- [ ] No remote draft/content path.
- [ ] Support diagnostics are structural/content-free.
- [ ] Dependency review has not introduced telemetry/cloud SDK accidentally.

## Lifecycle / privilege

- [ ] No automatic elevation/privileged helper/service was introduced for target coverage.
- [ ] Session-end/shutdown path is bounded and does not rely on a last-second full save.
- [ ] Double-start/login tests leave exactly one active observer/writer.

## Packaging

- [ ] Local data path/permissions verified.
- [ ] Upgrade migration tested without plaintext leak.
- [ ] Uninstall/reset behavior documented accurately.
- [ ] Release notes call out any support/profile privacy changes.
- [ ] MVP profile rules are release-bound; no unreviewed remote policy download exists.
- [ ] Update/migration temp paths pass plaintext-canary scan.

Any critical invariant failure blocks release.
