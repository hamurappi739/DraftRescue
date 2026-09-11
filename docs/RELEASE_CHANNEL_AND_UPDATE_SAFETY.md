# Release Channel and Update Safety

**Status:** pre-release policy baseline.

## 1. Stable channel principle

A stable release must never silently broaden which applications/fields are observed or weaken a security classifier through profile/config migration.

## 2. Profile updates

App profiles are security-sensitive executable policy inputs even if represented as data.

MVP baseline: profiles ship with the signed application release. No remotely downloaded profile rules in the first release.

A future remote profile-update system requires its own ADR covering signing, rollback, compatibility, privacy, and kill-switch behavior.

## 3. Configuration migrations

Migrations preserve the user's privacy choices. A new setting that increases capture surface defaults to the conservative value unless the product explicitly reviews otherwise.

## 4. Rollout gates

Before stable release/update:

- all critical privacy invariants green;
- supported app certification green for the exact supported version range;
- migration test from previous stable build;
- offline core functionality green;
- no plaintext canaries in install/update temp locations, logs, dumps intentionally produced by test harness, or DB;
- signing/package identity verified.

## 5. Emergency response

If a supported app/browser update breaks private-mode or secure-field detection, the affected profile must be disable-able in a subsequent signed release/config shipped with the product. MVP must fail closed on unknown/incompatible profile conditions.
