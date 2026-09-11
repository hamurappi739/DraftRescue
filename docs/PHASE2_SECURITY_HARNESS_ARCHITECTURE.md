# Phase 2 Security Harness Architecture

## Projects to add later

Recommended test-only projects when Phase 2 implementation begins:

```text
tests/DraftRescue.SecurityFixtures.Windows/   # Windows fixture executable
tests/DraftRescue.IntegrationTests.Windows/  # automation/integration runner
```

The fixture executable may use a Windows-native framework such as WinForms/WPF solely to expose deterministic accessibility surfaces. It is not a production dependency of DraftRescue.Desktop.

## Fixture pages

- Ordinary controls
- Protected controls
- Credential/PIN/payment synthetic controls
- Race/fault controls
- Version/profile identity display containing only product-owned fixture metadata

## IPC/control channel

If the test runner needs to command fixture faults, use a localhost/process-local test channel carrying only fixture commands/IDs, never entered field values. The harness must also be runnable fully offline.

## Counters

The fixture/test adapter must expose counters rather than content:

```text
metadata_requests
protected_signal_requests
policy_evaluations
capability_issue_attempts
capability_issue_successes
capability_claim_successes
content_read_invocations
```

For every Phase 2 run `content_read_invocations == 0`.

## Canary discipline

Synthetic values are obvious and non-secret. After fault runs, scan logs/artifacts for the canary. A canary outside the fixture's own test UI causes failure.
