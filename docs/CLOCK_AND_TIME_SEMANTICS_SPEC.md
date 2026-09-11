# Clock and Time Semantics

**Status:** accepted application contract.

## 1. Representation

Application/domain APIs use `DateTimeOffset`; persistence stores UTC normalized timestamps.

## 2. Clock abstraction

All expiry/scheduler tests use injectable `IClock` (or equivalent). Product logic does not scatter direct `DateTime.Now/UtcNow` calls.

## 3. Sliding retention

Each successfully accepted current snapshot updates `UpdatedAtUtc` and recalculates `ExpiresAtUtc` according to the active retention setting.

A stale/failed snapshot does not extend retention.

## 4. Wall-clock changes

Clock rollback must not intentionally revive a record already classified expired within the running process. Implementations may keep a monotonic/session expiry decision cache in addition to persisted wall-clock timestamps.

Clock jump forward may expire records earlier according to wall time; this is acceptable and safer than extending retention indefinitely.

## 5. Suspend/resume

On resume/wake:

- refresh current time;
- run expiry filter/cleanup before exposing recoverable list;
- invalidate stale observation handles/context;
- do not assume pre-suspend live target identity remains valid.

## 6. Display

UI displays localized relative/absolute time from metadata only. Presentation formatting does not change expiry authority.
