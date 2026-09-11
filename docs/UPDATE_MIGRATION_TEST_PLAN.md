# Update and Migration Test Plan

**Status:** Phase 10 packaging gate.

## Test fixture generations

Maintain synthetic data fixtures produced by representative prior schema/protection/profile versions. Fixtures contain unmistakably fake canary text only.

## Required paths

1. previous stable -> current stable;
2. empty installation -> current;
3. active unexpired drafts -> update;
4. expired drafts -> update;
5. one corrupted record -> update;
6. unsupported newer schema opened by older build -> safe refusal;
7. settings added/removed/renamed;
8. app profile version changes;
9. interrupted migration;
10. update with startup enabled/disabled.

## Privacy scans

During migration tests scan:

- application-data tree;
- installer/updater-controlled temp locations available to the test;
- logs;
- generated support diagnostics;

for plaintext canary strings.

## Assertions

- no bulk body decryption unless migration absolutely requires it and a new ADR approves it;
- no plaintext temp file;
- existing retention expiry preserved or shortened conservatively, never expanded silently;
- privacy setting is not broadened silently;
- profile support does not become more permissive due merely to migration;
- failed migration preserves recoverable previous state where possible;
- exactly one process owns observation after update/restart.
