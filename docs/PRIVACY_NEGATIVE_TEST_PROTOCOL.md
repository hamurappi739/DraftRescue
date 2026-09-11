# Privacy Negative-Test Protocol

## Goal

Test what DraftRescue must **not** do. Positive recovery tests do not substitute for these tests.

## Core assertion style

For a denied/unknown target, assert at the boundary:

```text
TextReader.InvocationCount == 0
Protector.InvocationCount == 0
Repository.UpsertCount == 0
Clipboard.WriteCount == 0
Network.SendCount == 0
```

Where relevant also assert:

```text
AllowedFieldHandleCount == 0
PlaintextCanaryInLogs == false
PlaintextCanaryOnDisk == false
```

## Negative suites

### Password

- IsPassword true;
- password signal arrives late;
- contradictory provider state;
- focus switches from allowed field to password field during pending job.

### Credentials/security codes

- synthetic login dialog;
- PIN field;
- OTP/security-code field;
- account-recovery secret field;
- unknown authentication surface.

### Banking/payment

- card number;
- CVV;
- bank account/IBAN-like field;
- payment dialog;
- unknown financial form.

### Private browsing

- private only;
- normal+private same process;
- private state unknown;
- browser update invalidates detector.

### Unsupported/uncertain

- unknown executable;
- unknown target version;
- ambiguous profiles;
- unsupported control;
- elevated target;
- UIA timeout;
- stale context generation.

## Canary discipline

Canaries are synthetic and unique to the test run. They must never resemble real user data. A failure report may include the canary identifier, not a production content sample.

## Release rule

A target cannot be marked supported when a required negative suite is skipped because the detector is difficult to test. Missing negative evidence blocks certification.
