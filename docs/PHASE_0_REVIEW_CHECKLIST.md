# Phase 0 Review Checklist

## Repository

- [ ] `DraftRescue.sln` opens.
- [ ] NuGet restore succeeds.
- [ ] Build succeeds with warnings treated as errors.
- [ ] Tests pass.
- [ ] Desktop app launches on Windows x64.

## Dependency boundaries

- [ ] Domain references no project/framework-specific UI or Windows code.
- [ ] Application references Domain only.
- [ ] Platform.Windows references Application + Domain.
- [ ] Infrastructure references Application + Domain.
- [ ] Desktop is the composition root.
- [ ] Tests reference Domain + Application only.

## Privacy

- [ ] No keyboard hook/global capture code exists.
- [ ] No UI Automation code exists yet.
- [ ] No persistence/database code exists yet.
- [ ] No clipboard code exists yet.
- [ ] No network/cloud/AI code exists.
- [ ] No sensitive content logging API exists.
- [ ] `CaptureEligibility.Uncertain().MayPersist` is false.
- [ ] `CaptureEligibility.SecureField().MayPersist` is false.
- [ ] `CaptureEligibility.PrivateBrowsing().MayPersist` is false.

## Scope

- [ ] No Phase 1+ runtime functionality was accidentally implemented.
