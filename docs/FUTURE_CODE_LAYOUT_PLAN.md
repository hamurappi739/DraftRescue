# Future Code Layout Plan

**Status:** implementation-ready directory proposal. Create directories only when their work package begins; do not prefill runtime stubs for later phases merely to match this tree.

```text
src/
  DraftRescue.Domain/
    Context/
      ApplicationId.cs
      ContextGeneration.cs
      SnapshotSequence.cs
      ContextFingerprintSet.cs
    Drafts/
      DraftId.cs
      DraftLifecycleState.cs
      CompletionAssessment.cs
      RecoverableState.cs
    Recovery/
      RecoveryMatchKind.cs
      RecoveryEvidenceSet.cs

  DraftRescue.Application/
    Contracts/
      Observation/
      Security/
      Reading/
      Tracking/
      Persistence/
      Profiles/
      Recovery/
      Retention/
      Clipboard/
      Settings/
      Time/
    UseCases/
      Observation/
      Drafts/
      Recovery/
      Settings/
    Models/
      CandidateFieldMetadata.cs
      AllowedFieldHandle.cs
      AllowedDraftSnapshot.cs
      ProtectedDraftRecord.cs
      ProtectedDraftMetadata.cs
    Errors/
      DraftRescueErrorCode.cs
      DraftRescueError.cs

  DraftRescue.Platform.Windows/
    Observation/
      WinEventForegroundContextSource.cs
    Automation/
      UiaWorker.cs
      UiaCandidateFieldLocator.cs
      UiaEligibleFieldTextReader.cs
      UiaRestoreTargetLocator.cs
      Adapters/
    BrowserPrivacy/
      ChromiumPrivateModeDetector.cs     # only after validated work package
    Startup/
    SingleInstance/

  DraftRescue.Infrastructure/
    Persistence/
      SqliteDraftRepository.cs
      Schema/
      Migrations/
    Protection/
      DpapiDraftProtector.cs
      InstallationSecretStore.cs
    Fingerprinting/
      HmacFingerprintService.cs
    Settings/
    Paths/

  DraftRescue.Desktop/
    ViewModels/
      RecoveryPageViewModel.cs
      DraftCardViewModel.cs
      PreviewDraftViewModel.cs
      SettingsViewModel.cs
    Views/
    Services/
      TrayUiService.cs
    Composition/

  DraftRescue.TestHarness/             # create when integration work begins
    SyntheticFields/
    Scenarios/

tests/
  DraftRescue.Tests/
    Architecture/
    Domain/
    Application/
    Persistence/
    Security/
    Recovery/
    Profiles/
```

## Rules

- folders reflect responsibility, not framework jargon;
- do not create a giant `Helpers` or `Utils` directory;
- do not put application decisions into `Platform.Windows`;
- do not put concrete database/protection implementations into `Application`;
- do not let Desktop contain `AutomationElement`, SQLite connection, or DPAPI calls;
- synthetic test harness is never fed real user data.
