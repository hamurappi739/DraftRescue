# One-Shot Eligible Text Reader Contract

## Contract

Conceptual API:

```csharp
Task<EligibleTextReadResult> ReadOnceAsync(
    AllowedFieldHandle handle,
    ReadBudget budget,
    CancellationToken cancellationToken);
```

The implementation accepts no raw `AutomationElement`, hwnd, process id, or user text from Application code.

## Result union

- `Success(FieldTextSnapshot snapshot)`
- `Empty(FieldTextSnapshot snapshot)`
- `TooLarge(observedAtLeast, configuredLimit)`
- `Expired`
- `AlreadyConsumed`
- `RevokedOrStale`
- `TargetChanged`
- `UnsupportedReadStrategy`
- `Timeout`
- `ProviderFailure(errorCode)`
- `Cancelled`

No failure result contains target text.

## Capability semantics

Calling the reader consumes the capability before content access. Retrying requires fresh Phase-2 classification and a newly issued handle. The reader never internally reissues permissions.

## Cancellation

Cancellation is best-effort. If a provider call cannot be interrupted, its late result is discarded after deadline/generation checks. Cancellation never returns a partial string as a valid snapshot.

## Logging

Allowed: strategy enum, timing bucket, outcome enum, length bucket after successful bounded read. Forbidden: text, prefixes/suffixes, hashes of plaintext, raw provider exceptions if they can contain UI text.
