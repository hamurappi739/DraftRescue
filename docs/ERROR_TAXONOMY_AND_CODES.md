# Error Taxonomy and Stable Codes

**Status:** accepted diagnostic contract v1.

## 1. Goals

DraftRescue needs actionable diagnostics without leaking user text. Errors therefore use stable content-free codes plus a bounded set of safe structural fields.

A diagnostic error contains conceptually:

```text
DraftRescueError
- Code
- Category
- Severity
- RetryDisposition
- SafeContextFields
- InnerExceptionType?   // type only; message is not automatically logged
```

Never attach draft text, preview text, clipboard content, raw URL, raw page title, raw accessibility name, DPAPI payload bytes, or arbitrary exception `Data`.

## 2. Categories

| Prefix | Area |
|---|---|
| `DR-OBS` | foreground/focus/metadata observation |
| `DR-SEC` | security/private/support denial |
| `DR-READ` | eligible text snapshot read |
| `DR-TRK` | draft tracking/current-state orchestration |
| `DR-PRO` | payload protection/unprotection |
| `DR-DB` | persistence/schema/corruption |
| `DR-PRF` | app-profile resolution/compatibility |
| `DR-MAT` | recovery matching |
| `DR-RST` | restore/write/verification |
| `DR-CLP` | explicit clipboard path |
| `DR-CFG` | settings/configuration |
| `DR-IPC` | single-instance command IPC |
| `DR-UPD` | install/update/migration |

## 3. Core codes

### Observation

- `DR-OBS-1001` — foreground context unavailable
- `DR-OBS-1002` — no supported editable candidate
- `DR-OBS-1003` — stale context result ignored
- `DR-OBS-1004` — UIA/provider timeout
- `DR-OBS-1005` — UIA/provider failure

### Security/policy

These are normally policy outcomes rather than UI errors.

- `DR-SEC-2001` — protected/password field denied
- `DR-SEC-2002` — credential-like field denied
- `DR-SEC-2003` — banking/payment-like field denied
- `DR-SEC-2004` — private browsing denied
- `DR-SEC-2005` — uncertain security state denied
- `DR-SEC-2006` — unsupported integrity boundary denied
- `DR-SEC-2007` — unsupported app/control policy denied

### Read

- `DR-READ-3001` — allowed handle expired
- `DR-READ-3002` — target changed before read
- `DR-READ-3003` — read capability unsupported
- `DR-READ-3004` — eligible read timed out
- `DR-READ-3005` — eligible read failed

### Tracking

- `DR-TRK-4001` — stale snapshot sequence ignored
- `DR-TRK-4002` — invalid draft transition rejected
- `DR-TRK-4003` — active draft identity conflict

### Protection

- `DR-PRO-5001` — protect failed
- `DR-PRO-5002` — unprotect failed
- `DR-PRO-5003` — unknown protection version
- `DR-PRO-5004` — installation secret unavailable
- `DR-PRO-5005` — protected payload envelope invalid
- `DR-PRO-5006` — installation secret corrupt/incompatible

### Database

- `DR-DB-6001` — store open failed
- `DR-DB-6002` — transaction failed
- `DR-DB-6003` — store corruption detected
- `DR-DB-6004` — schema incompatible
- `DR-DB-6005` — stale protected-record upsert ignored
- `DR-DB-6006` — migration failed
- `DR-DB-6007` — bounded database busy timeout
- `DR-DB-6008` — equal-sequence contradictory record conflict
- `DR-DB-6009` — store quarantined/unavailable

### Profile

- `DR-PRF-7001` — no matching profile
- `DR-PRF-7002` — target version not certified
- `DR-PRF-7003` — profile explicitly blocked
- `DR-PRF-7004` — ambiguous profile match
- `DR-PRF-7005` — invalid manifest

### Recovery matching

- `DR-MAT-8001` — live target unavailable
- `DR-MAT-8002` — ambiguous target
- `DR-MAT-8003` — required anchor missing
- `DR-MAT-8004` — fatal evidence conflict
- `DR-MAT-8005` — target security state unsafe

### Restore

- `DR-RST-9001` — target changed during restore
- `DR-RST-9002` — write capability unsupported
- `DR-RST-9003` — write failed
- `DR-RST-9004` — verification mismatch
- `DR-RST-9005` — applied but verification unavailable
- `DR-RST-9006` — restore already in flight for draft

### Clipboard/config/IPC/update

- `DR-CLP-10001` — clipboard unavailable
- `DR-CLP-10002` — clipboard write failed
- `DR-CFG-11001` — settings invalid
- `DR-CFG-11002` — settings persistence failed
- `DR-IPC-12001` — primary instance unavailable
- `DR-IPC-12002` — unsupported IPC command
- `DR-UPD-13001` — migration precondition failed
- `DR-UPD-13002` — migration interrupted/rolled back

## 4. Safe diagnostic fields

Allowed when relevant:

```text
error_code
component
operation
app_profile_id
app_profile_version
target_version_bucket
control_type_enum
framework_hint_enum
context_generation
snapshot_sequence
elapsed_ms
retry_count
support_state
result_category
```

Never use these fields as a backdoor for arbitrary strings.

## 5. Retry disposition

Each code maps to one of:

- `NoRetry` — policy deny, invalid input, deterministic unsupported state;
- `RetryWithBackoff` — transient provider/clipboard/store availability;
- `RetryAfterContextChange` — stale/changed target;
- `UserActionRequired` — explicit retry/reopen/reselect is needed;
- `FatalForFeature` — corruption/schema/protection issue disables affected feature safely.

Security denials are never retried in a tight loop merely hoping to become allowed.

## 6. User-facing messages

Ordinary UI uses concise product copy, not internal exception messages. A diagnostic code may be shown behind an optional `Details` affordance when useful, e.g. `DR-RST-9004`, but not raw provider text.

## 7. Exception handling rule

Do not log `exception.Message` blindly for platform/provider exceptions because third-party accessibility providers may include user-derived text in messages. Prefer exception type + HRESULT/category + stable code unless a message source has been specifically audited as content-free.
