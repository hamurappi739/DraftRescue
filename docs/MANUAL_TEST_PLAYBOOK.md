# Manual Test Playbook

Use **synthetic text only**. Never test DraftRescue by typing real passwords, card data, private messages, or production secrets.

## Test canary

Generate a unique string per run, for example:

```text
DR_CANARY_2026_<random>
```

The canary is deliberately non-secret test data used to detect accidental plaintext leakage.

# Phase 1 playbook

1. Start DraftRescue diagnostic test build.
2. Open Notepad/test harness.
3. Switch foreground between target, Explorer, browser, DraftRescue.
4. Verify app identity/focused metadata updates.
5. Verify no text value appears in console/logs/storage.
6. Rapidly Alt+Tab for several minutes.
7. Verify CPU/handles/subscriptions stabilize.

# Phase 2 playbook

Controlled fixture should include:

- ordinary TextBox/Edit;
- password box;
- read-only text;
- custom/unsupported control;
- simulated provider failure.

Verify only ordinary explicitly supported field can reach Allowed.

# Phase 3 playbook

With synthetic canary:

1. type in safe supported field;
2. switch focus away/back — current draft survives;
3. replace all text — only replacement remains current;
4. clear field intentionally — old text should not later appear as recovery;
5. type again, abruptly close target — latest current text becomes recoverable in memory.

# Phase 4 playbook

1. type canary in safe fixture;
2. wait for checkpoint;
3. kill DraftRescue/target at varied times;
4. restart;
5. verify expected current snapshot recovery;
6. inspect DraftRescue-owned logs/storage with text search — canary must not appear plaintext;
7. let retention expire with app closed; restart; draft must be gone.

# Phase 5 playbook

- Empty state is quiet/simple.
- Preview shows local canary only after user action.
- Preview does not mutate clipboard.
- Discard removes item permanently.
- Error messages contain no raw exception/content.

# Phase 6 playbook

Create two similar text fields.

- correct strong match -> Restore works;
- focus second field -> direct Restore refuses/aborts;
- turn target into password fixture -> refuses;
- close target after click -> no injection;
- simulated write failure -> draft remains recoverable;
- confirmed success -> recoverable item removed.

# Browser playbook

Use a local/synthetic test page, never real accounts.

For Chrome/Edge test:

- normal editor positive;
- Incognito/InPrivate negative;
- browser omnibox negative;
- synthetic login/password negative;
- synthetic payment negative;
- two origins with identical layout: cross-origin restore negative;
- normal and private windows simultaneously;
- browser restart/update fallback behavior.

# Final privacy sweep

Search only DraftRescue-owned directories/logs for current canary. It must appear only where explicitly expected (for example decrypted UI memory cannot be file-scanned) and never in plaintext persistent artifacts.
