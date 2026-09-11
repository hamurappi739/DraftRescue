# Retention Policy Specification

## 1. Product options

Baseline retention choices:

- 5 minutes
- 30 minutes
- 1 hour
- 6 hours
- 24 hours
- custom

MVP default: **30 minutes**.

## 2. Meaning

Retention starts from the most recent eligible draft update (`UpdatedAt`), not application install time or first-ever observation.

Conceptually:

```text
ExpiresAt = UpdatedAt + RetentionDuration
```

MVP uses sliding expiry only. There is no separate absolute-lifetime cap; the selected retention duration itself is bounded.

## 3. Bounds for custom retention

Custom retention must be bounded. MVP should not permit “forever.”

Accepted MVP bounds:

- minimum: 1 minute;
- maximum: 24 hours.

There is no `forever` value. Longer values require a new privacy/product ADR because long retention begins to resemble history storage.

## 4. Expiry enforcement

Expiry is enforced in multiple places:

- when loading storage at startup;
- periodic lightweight cleanup while running;
- before Preview/Copy/Restore returns plaintext;
- before recovery UI presents stale cached records where practical.

Do not rely on one background timer as the sole privacy boundary.

## 5. Retention change behavior

When user shortens retention:

- recalculate eligibility of existing recoverable drafts against the new duration;
- immediately delete records now outside the new window.

When user lengthens retention:

- do not resurrect already expired/deleted drafts;
- existing still-live drafts may adopt the new duration according to final application policy.

## 6. Clock changes

Store UTC timestamps. Wall-clock jumps can happen.

Policy should remain simple and privacy-biased:

- if a record is already past `ExpiresAt`, delete it;
- do not extend expired records because the clock later moves backwards;
- tests cover forward/backward clock adjustments.

## 7. UI

UI displays approximate friendly expiry text, but application logic owns the authoritative timestamp.

Examples:

- `Expires in 24 min`
- `Expires soon`

Do not expose second-by-second countdowns that cause needless UI updates.

## 8. Deletion

Expiry permanently removes the recoverable payload. No hidden recycle bin/history is created.
