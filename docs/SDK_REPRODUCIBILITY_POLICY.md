# .NET SDK Reproducibility Policy

## Status

DraftRescue targets `net8.0` and requires a **Windows x64 .NET 8 SDK**, not merely the .NET 8 runtime.

`global.json` declares:

- baseline SDK: `8.0.100`;
- `rollForward: latestFeature`;
- prerelease SDKs disabled.

This intentionally allows a stable installed .NET 8 feature band while preventing silent execution under a different major runtime family.

## Phase-0 verification rule

`scripts/verify.ps1` must fail if:

- `dotnet` is absent;
- `dotnet --info` fails;
- no installed SDK begins with `8.0.`;
- restore, build, or test returns a non-zero exit code.

`scripts/verify.cmd` must likewise stop on the first failing command.

A runtime installation without an SDK is not sufficient.

## First green Windows build

After the first successful Windows Phase-0 verification, record the exact SDK version actually used in the Phase-0 verification report. Do not tighten `global.json` to a narrower patch or feature band unless that narrower requirement has been deliberately tested and accepted.

## CI

When CI is introduced, it must execute the same Phase-0 verification gate on a compatible stable .NET 8 SDK and report the resolved `dotnet --version` / `dotnet --info` output.
