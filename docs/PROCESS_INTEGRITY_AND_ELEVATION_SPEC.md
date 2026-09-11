# Process Integrity, Elevation, and Cross-Process Boundary Specification

**Status:** safety baseline for Windows platform layer.

## 1. Principle

DraftRescue runs as a normal user desktop utility. It must not request administrator elevation just to broaden observation/restore coverage.

Unsupported privilege boundaries are preferable to weakening Windows isolation.

## 2. Elevated target applications

If DraftRescue cannot safely inspect or write to a higher-integrity/elevated target:

- classify the target as unsupported/unavailable for the relevant capability;
- do not relaunch DraftRescue elevated automatically;
- do not inject a helper into the target;
- do not use lower-level input interception as a fallback.

Recovery UI may still expose an existing stored draft through Preview/Copy if that action itself remains safe.

## 3. Secure desktop

DraftRescue must not attempt observation on the Windows secure desktop, UAC consent UI, lock screen, credential UI, or equivalent protected surfaces.

## 4. Session boundaries

MVP operates only in the current interactive user's session. No service, cross-user observer, session-0 component, or machine-wide text broker.

## 5. IPC

Any future helper process must require a dedicated ADR and threat review. Default architecture has no privileged helper.

## 6. Test requirements

- normal app target;
- Run as administrator target;
- UAC prompt transition;
- lock/unlock;
- fast user switching if supported by test environment;
- second Windows user cannot access first user's protected records through normal app operation.
