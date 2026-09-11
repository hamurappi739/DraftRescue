# Data Flow & Plaintext Boundaries

## 1. Purpose

This document answers the most important implementation question: **where is user text allowed to exist?**

## 2. Pipeline

```text
Windows event / foreground change
        |
        v
[Platform.Windows]
metadata-only candidate discovery
        |
        v
[Application]
SecureInputGuard + AppProfile checks
        |
   deny | allow
        |   |
        |   v
        | [Platform.Windows]
        | minimum field snapshot read
        |   |
        |   v
        | [Application]
        | DraftTracker current state
        |   |
        |   v
        | [Infrastructure crypto boundary]
        | plaintext -> protected payload
        |   |
        |   v
        | [Infrastructure repository]
        | ciphertext + minimal metadata only
        |
        +--> DROP without content persistence
```

## 3. Components allowed to see plaintext

Only components that require it for product function:

- eligible field snapshot reader after allow;
- DraftTracker current in-memory state;
- protector input immediately before encryption;
- Preview application/UI path after explicit local decryption;
- explicit Copy path;
- explicit Restore path.

## 4. Components that must not see plaintext

- foreground application detector;
- candidate metadata locator before allow;
- SecureInputGuard;
- AppProfile resolver for normal classification;
- ordinary structured logger;
- repository storage engine API when a protected record can be used instead;
- retention scheduler;
- recovery list query when preview text is not needed;
- metrics/performance tracing;
- update/packaging subsystem.

## 5. Plaintext lifetime

Implementation should minimize lifetime rather than pretend managed-memory strings can be perfectly wiped.

Rules:

- do not make unnecessary copies;
- do not cache decrypted previews globally;
- do not include plaintext in exception messages;
- do not serialize plaintext to temp/config/log files;
- release references promptly after Preview/Copy/Restore operations;
- avoid long-lived singleton services holding current text when a bounded session object works.

## 6. UI

Displaying Preview necessarily places plaintext in UI process memory and rendered surfaces. This is an intentional user-requested local action, not a reason to duplicate the text elsewhere.

UI must never bind user text to logging/telemetry properties.

## 7. Clipboard

Clipboard is outside DraftRescue's protected storage boundary. Copy is explicit user action and may be observed/synced by OS/third-party clipboard services.

Never use it silently as a persistence mechanism.

## 8. Crash dumps

Process memory can appear in OS crash dumps. DraftRescue should avoid adding its own automatic dump/upload pipeline. A later hardening review may evaluate Windows Error Reporting implications, but application logs/support bundles must never intentionally include plaintext.

## 9. Code review question

For every new parameter/property containing draft text, reviewer asks:

> Does this component truly need plaintext to perform its responsibility?

If not, replace it with metadata/opaque ID/protected payload.
