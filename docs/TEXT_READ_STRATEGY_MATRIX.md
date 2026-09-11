# Text Read Strategy Matrix

## Baseline

| Surface | Strategy | Phase 3 status | Reason |
|---|---|---|---|
| certified multiline/document provider | `TextPattern.DocumentRange.GetText(limit + 1)` | allowed | API supports bounded `maxLength` |
| certified single-line/value control | `ValuePattern.Current.Value` | allowed only by explicit profile certification | provider returns the whole string; no API-side maxLength |
| TextPattern unsupported + ValuePattern unsupported | none | deny/unsupported | no generic fallback |
| LegacyIAccessible value | none | forbidden generic fallback | expands compatibility/privacy surface |
| keyboard/clipboard scrape | none | forbidden | violates architecture/privacy |

## Selection rule

The profile/adapter declares a read strategy as part of certified capability. Runtime does not probe a chain of content-bearing patterns until one happens to work.

## TextPattern

For document/multiline surfaces, request `DocumentRange`, then `GetText(maxChars + 1)`. Never use `-1` in production. `maxChars + 1` allows a typed `TooLarge` result without accepting silent truncation.

## ValuePattern

Microsoft documents ValuePattern as the normal content access for single-line edit controls while multiline controls typically require TextPattern. Because `ValuePattern.Value` returns a complete string, it is allowed only for certified surfaces with realistic bounded input behavior. A managed post-read hard cap still rejects an oversized result.

## No fallback ladder

Failure of the certified strategy is a typed failure. It does not trigger TextPattern -> ValuePattern -> LegacyIAccessible -> keyboard fallback guessing.
