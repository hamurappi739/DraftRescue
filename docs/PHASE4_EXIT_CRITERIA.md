# Phase 4 Exit Criteria

Phase 4 is complete only when every mandatory criterion passes on the Windows target environment.

## Architecture

- [ ] repository cannot accept plaintext type;
- [ ] DPAPI implementation isolated behind `IDraftProtector`;
- [ ] SQLite implementation isolated from UI/Windows UIA;
- [ ] one logical writer queue;
- [ ] one current row per `DraftId`.

## Protection

- [ ] DPAPI `CurrentUser` roundtrip works;
- [ ] Protect failure causes zero repository writes;
- [ ] unknown protection version fails closed;
- [ ] installation HMAC secret is random and DPAPI-protected;
- [ ] secret-loss-with-existing-DB does not silently regenerate identity.

## SQLite

- [ ] schema v1 exact constraints validated;
- [ ] `journal_mode=DELETE` verified;
- [ ] `secure_delete=ON` verified;
- [ ] `synchronous=EXTRA` verified or replaced only by approved measured ADR;
- [ ] bounded busy timeout verified;
- [ ] crash tests demonstrate valid old/new transaction state;
- [ ] stale sequence cannot overwrite newer row.

## Privacy

- [ ] raw DB/journal/temp/log/settings scan contains zero synthetic plaintext canaries;
- [ ] no raw URL/title/field label persistence;
- [ ] list recoverables decrypt count = 0;
- [ ] expiry cleanup decrypt count = 0;
- [ ] discard-all decrypt count = 0;
- [ ] no revision/history schema.

## Retention / failures

- [ ] expired row hidden even if cleanup delete fails;
- [ ] corruption path does not salvage or upload;
- [ ] unknown DB schema does not auto-drop/recreate;
- [ ] migration failure is fail-closed and transactional where specified;
- [ ] disk-full/locked-store failures are bounded.

## Performance

- [ ] checkpoint frequency follows Phase-3/4 scheduler rather than every keystroke;
- [ ] 30-minute synthetic editing soak has bounded rows/files/memory;
- [ ] no hot retry loop while store unavailable.

## Stop condition

After these gates pass, **STOP**. Phase 5 explicit Preview/UI decryption is a separate phase and must not be bundled into Phase 4.
