# User-Visible Failure Copy Matrix

**Status:** initial MVP copy baseline. Internal `DR-*` codes remain available for diagnostics but raw exception messages are not shown.

| Situation | User-facing copy | Actions |
|---|---|---|
| recovery target unavailable | `The original field isn't available right now.` | Preview, Copy |
| ambiguous target | `DraftRescue couldn't safely identify the original field.` | Preview, Copy |
| target became unsafe/private | `This draft can't be restored to the current field safely.` | Preview, Copy |
| target changed during restore | `The field changed before the draft could be restored.` | Preview, Copy, Retry when appropriate |
| direct write unsupported | `Automatic restore isn't available for this field.` | Copy |
| write failed before success | `DraftRescue couldn't restore this draft. Your recovery copy was kept.` | Preview, Copy, Retry |
| applied but unverified | `The draft was inserted, but DraftRescue couldn't verify the result. Your recovery copy was kept.` | Preview, Copy, Discard if user confirms |
| verification mismatch | `The field didn't match the restored draft. Your recovery copy was kept.` | Preview, Copy |
| draft expired between list and action | `This draft has expired and is no longer available.` | Close/remove card |
| payload cannot be decrypted | `This draft can't be opened.` | Discard; optional diagnostic Details |
| local store unavailable | `Recoverable drafts are temporarily unavailable.` | Retry/Open Settings if relevant |
| clipboard unavailable | `Couldn't copy the draft to the clipboard.` | Retry |
| invalid retention value | `Choose a retention time between 1 minute and 24 hours.` | Edit value |
| quitting app | `Quit DraftRescue? New drafts won't be protected until it starts again.` | Cancel, Quit |

## Copy rules

- never mention internal handles, UIA, DPAPI, SQLite, fingerprints, scores, or process IDs in ordinary text;
- never paste raw provider error messages into dialogs;
- avoid implying a draft is permanently safe if persistence just failed;
- avoid blaming the user;
- do not say `Restored` unless result is `VerifiedRestored`;
- internal diagnostic code can appear behind optional Details without extra raw context.
