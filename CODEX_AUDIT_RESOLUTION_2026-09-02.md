# Codex Foundation Audit Resolution — 2026-09-02

This document records the disposition of `CODEX_FOUNDATION_AUDIT_2026-09-02.md`.

## F-001 — `verify.ps1` false success

**Resolved in package.**

`scripts/verify.ps1` now checks native exit codes after `dotnet --info`, `dotnet --list-sdks`, restore, build and test. It also fails when no compatible .NET 8 SDK is installed and prints the success banner only after every mandatory command succeeds.

Acceptance still requires execution on Windows. A deliberate failing-path check should be performed by running without a compatible SDK or by temporarily supplying an invalid solution path and confirming a non-zero process exit and no success banner.

## F-002 — Phase-0 gate not actually passed

**Resolved in package on 2026-09-03.**

The real Windows x64 `.NET 8 SDK` restore/build/test gate passed with SDK 8.0.424. The Avalonia shell was launched successfully and its process was observed. Phase 0 is green.

## F-003 — stale manifest/count metadata

**Resolved in package.**

The inventory, manifest, and checksum list are regenerated together when this handoff is built.

Checksum policy:

- `FOUNDATION_SHA256SUMS.txt` excludes itself to avoid a recursive checksum definition;
- every other regular file in the ZIP is included, including `FOUNDATION_FILE_INVENTORY.txt` and `FOUNDATION_MANIFEST.md`;
- `FOUNDATION_FILE_INVENTORY.txt` lists every regular file in the package, including itself and the checksum file.

## F-004 — SDK reproducibility not fixed

**Resolved at family/range level; exact tested SDK remains evidence to record after the first green Windows build.**

`global.json` now declares `8.0.100` with `rollForward: latestFeature` and `allowPrerelease: false`. `scripts/verify.ps1` additionally requires an installed `8.0.*` SDK. See `docs/SDK_REPRODUCIBILITY_POLICY.md`.

## Remaining blocker

No Phase-0 blocker remains. Phase 1 WP-1.1, WP-1.2 and WP-1.3 are implemented; the next allowed scope is WP-1.4 after review of the WP-1.3 coordinator evidence.
