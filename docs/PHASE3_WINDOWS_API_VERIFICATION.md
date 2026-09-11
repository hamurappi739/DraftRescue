# Phase 3 Windows API Verification

Verified against Microsoft UI Automation documentation before implementation.

## ValuePattern

`ValuePattern.ValuePatternInformation.Value` returns the control value as a string. Microsoft notes that single-line edit controls commonly expose content via ValuePattern, while multiline edit controls generally use TextPattern. ValuePattern has no bounded-length read parameter.

Implication: use only on certified bounded single-line surfaces; reject oversized result after read.

## TextPattern

`TextPattern.DocumentRange` yields the range enclosing the main document text. `TextPatternRange.GetText(int maxLength)` returns plain text with a caller-specified maximum; `-1` means unlimited.

Implication: production DraftRescue uses a finite `maxLength`, specifically `configuredLimit + 1`, never `-1`.

## TextPattern limitation

TextPattern is a read-oriented text model and does not itself provide generic text insertion. Restore strategy is a separate later-phase concern.

## Implementation verification checklist

- confirm actual control pattern availability on certified Windows/target versions;
- measure timeout/provider behavior on dedicated UIA MTA worker;
- verify TextPattern provider's returned line endings/artifacts with synthetic fixtures;
- never infer safety from pattern availability.
