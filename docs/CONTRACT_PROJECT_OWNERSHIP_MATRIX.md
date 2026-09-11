# Contract / Project Ownership Matrix

**Status:** accepted placement baseline for future implementation.

| Concern / type | Owning project | May depend on | Must not depend on |
|---|---|---|---|
| `DraftId`, lifecycle enums, fingerprint value objects | `DraftRescue.Domain` | BCL only | Avalonia, UIA, SQLite, DPAPI |
| `ContextGeneration`, `SnapshotSequence` | `DraftRescue.Domain` | BCL only | platform/infrastructure |
| use-case commands/queries/results | `DraftRescue.Application` | Domain | Avalonia, concrete SQLite/DPAPI/UIA |
| `ISecureInputGuard`, `IDraftTracker`, `IRecoveryMatcher` | `DraftRescue.Application` | Domain | Windows implementations |
| profile domain/contracts + resolver abstraction | `DraftRescue.Application` | Domain | manifest filesystem details where avoidable |
| Win32 foreground/focus source | `DraftRescue.Platform.Windows` | Application/Domain contracts | Desktop ViewModels |
| UI Automation metadata/read/write adapters | `DraftRescue.Platform.Windows` | Application/Domain contracts | SQLite/Avalonia UI logic |
| DPAPI protector | `DraftRescue.Infrastructure` or Windows infrastructure adapter | Application/Domain contracts | ViewModels |
| SQLite repository | `DraftRescue.Infrastructure` | Application/Domain contracts | Avalonia/UIA |
| settings file/store | `DraftRescue.Infrastructure` | Application contracts | UIA |
| tray/startup registration implementation | `DraftRescue.Platform.Windows` / package-specific adapter | Application contract | draft body |
| Views/ViewModels | `DraftRescue.Desktop` | Application use cases/results | repository, DPAPI, UIA objects |
| composition root | `DraftRescue.Desktop` | all implementation projects | business logic beyond wiring |
| synthetic fixture app/harness | test project/tooling | public contracts | real user data |

## Placement rule

If a proposed class needs both Avalonia types and SQLite/DPAPI/UI Automation types, the design is probably crossing boundaries incorrectly. Split the responsibility before coding.

## Plaintext rule

Plaintext types may cross only the narrow application operation paths defined in `CONTENT_REVEAL_AND_MEMORY_LIFETIME_SPEC.md`. They are not general DTOs shared across layers.
