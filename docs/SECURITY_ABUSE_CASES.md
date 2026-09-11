# Security & Abuse Cases

This document describes ways a superficially working implementation could violate the product.

## A1. Hidden keylogger architecture

**Bad design:** install a global keyboard hook, reconstruct text for every app, later decide which parts to save.

**Why rejected:** unrelated/sensitive input crosses the collection boundary.

**Required defense:** field/context-level metadata gating before content access; no general keystroke stream.

## A2. Password filter after capture

**Bad design:** read `Value` first, then check `IsPassword`.

**Required defense:** query protected/security metadata first whenever platform permits. If an API couples them, treat adapter as high-risk and immediately discard denied content without logs/persistence.

## A3. “IsPassword=false means safe”

**Bad design:** allow all non-password fields.

**Required defense:** positive supported-field policy plus credential/banking/private/unsupported checks.

## A4. Private-browser title heuristic breaks

**Bad design:** detect private mode from one English title suffix; browser update/localization defeats it.

**Required defense:** profile-specific multi-signal detection; unknown browser privacy state denies.

## A5. Recovery to wrong field

**Bad design:** same process/window title is enough to inject.

**Required defense:** multi-signal fresh match + fresh secure guard + no force restore.

## A6. Stale permission race

**Bad design:** field was safe at T1, user tabs into password field at T2, cached allow is reused.

**Required defense:** field-scoped short-lived permission and identity revalidation before content read/restore.

## A7. History by accident

**Bad design:** append each snapshot to database “for reliability.”

**Required defense:** upsert latest state only; tests assert bounded record count for one logical draft.

## A8. Deleted text resurrected

**Bad design:** user clears a message intentionally, DraftRescue later offers the old version.

**Required defense:** stable manual clear removes current active snapshot; no undo/history copy by default.

## A9. Secret leaks through logs

**Bad design:** platform exception/debug object prints `Value`, full automation tree, URL, or draft.

**Required defense:** safe error categories and canary leakage tests.

## A10. Plaintext temp file

**Bad design:** encrypted DB exists, but preview/export/cache creates `%TEMP%` plaintext.

**Required defense:** no plaintext temp artifacts for normal operation; canary scan product-owned files.

## A11. Clipboard surprise

**Bad design:** restore failure automatically copies draft; clipboard manager/sync now receives it.

**Required defense:** Copy is explicit only; no automatic clipboard fallback.

## A12. Over-broad app profile

**Bad design:** process `chrome.exe` marked safe, so omnibox/login/payment fields become eligible.

**Required defense:** profile identifies supported page editor surfaces, never whole process safety.

## A13. Provider hang causes polling storm

**Bad design:** UIA timeout triggers rapid retries and high CPU.

**Required defense:** bounded timeout, cancellation, backoff/coalescing, uncertain deny.

## A14. Retention bypass on app downtime

**Bad design:** cleanup timer only runs while process is alive; week-old drafts return after restart.

**Required defense:** startup expiry cleanup before recovery presentation.

## A15. Corrupt ciphertext rendered/logged

**Bad design:** decryption failure bytes/string appear in UI/log.

**Required defense:** generic corruption result, opaque ID only, no payload output.
