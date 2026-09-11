# Text Length and Truncation Policy

## Product rule

DraftRescue never silently stores a truncated draft as though it were complete.

## Baseline limits

- default certified-target logical maximum: **65,536 UTF-16 code units**;
- profile may lower this limit;
- profile may raise it only with explicit certification evidence;
- absolute MVP hard cap: **262,144 UTF-16 code units**.

The hard cap is a safety ceiling, not a promise that every target supports drafts this large.

## TextPattern bounded read

Use `GetText(configuredLimit + 1)` where the integer limit is within the hard cap. If returned length exceeds configured limit, result is `TooLarge`; plaintext is discarded and tracker is not updated.

## ValuePattern caveat

`ValuePattern.Value` has no `maxLength` argument. Therefore it is permitted only on a certified surface whose expected content size is bounded. If the returned string exceeds configured limit, return `TooLarge` and do not track it. The memory allocation has already happened, so extremely large/unbounded ValuePattern surfaces are not eligible for early certification.

## No partial recovery

`TooLarge` does not create a partial current draft. Future product work may introduce explicit large-draft support only through a separate decision/ADR.
