# WP-1.4 WPF interactive confirmation runbook

This runbook is the bounded, metadata-only procedure for the last Phase-1
criterion (`DR-PHASE1-0003`). It is deliberately separate from production
capture and must be run in a real interactive Windows desktop.

## Before starting

- Build the Release harness once if needed:

```powershell
dotnet build .\experiments\DraftRescue.Phase1Harness\DraftRescue.Phase1Harness.csproj --no-restore -c Release
```

- Close stale `DraftRescue.Desktop` or harness processes so the binary can be
  rebuilt. Do not close unrelated applications.
- Keep the synthetic fixture empty. Do not type, paste, or copy any text.

## One-command review

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_wpf_interactive_review.ps1
```

The runner writes one combined record to
`artifacts/phase1-wpf-interactive-review/WP14-WPF-INTERACTIVE-REVIEW.json`.
It first records session readiness, then opens the synthetic fixture. When the
terminal says `Click the empty test field, then press Enter here`, click only
the empty field and press Enter in the same terminal. The bounded probe then
closes the fixture and writes its metadata-only result.

## Interpreting the result

- `Pass` requires fixture-focused metadata, `Edit`/`Wpf` structural
  classification and zero content-reader calls.
- `Inconclusive` with `ActivationBlocked` means Windows did not verify the
  fixture as foreground; it does not authorize relaxing the gate.
- `MetadataReadButForeignFocus` means UIA returned structural metadata for a
  different focused process; this is still not a capture permission.
- Any safety-audit failure is a hard stop and must be investigated before
  rerunning.

## Fast readiness smoke

To inspect the desktop without launching the fixture or waiting for input:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_wpf_interactive_review.ps1 -PreflightOnly
```

This mode is diagnostic only and never closes `DR-PHASE1-0003`.

## Privacy boundary

The procedure reads only process/session/window handles and structural UIA
metadata. It does not read window titles, text/value patterns, clipboard data,
keyboard streams, passwords, private-browser content, network data or stored
drafts. Relevant invariants: P-001, P-002, P-003, P-026, P-028, P-029, P-030,
C-016, C-017, C-022, C-023.
