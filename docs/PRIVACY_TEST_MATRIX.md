# Privacy Test Matrix — Planned

**Status:** test plan for later phases. No claim is made that these cases are currently implemented.

| Area | Case | Expected behavior |
|---|---|---|
| Windows controls | ordinary explicitly supported editable field | eligible only after explicit guard allow |
| Windows controls | password/protected edit | text reader not invoked where pre-read gating is possible; never persist |
| Windows controls | read-only text/document | unsupported for draft capture |
| Windows controls | custom control with incomplete metadata | uncertain/deny |
| Credentials | username/password authentication surface | credential/security rules deny relevant fields |
| Security codes | PIN/OTP/passcode/CVV-like surface from metadata/profile | deny |
| Banking | payment/banking credential field | deny |
| Uncertainty | UIA provider throws/times out | deny, no permissive fallback |
| Race | allowed field replaced by password field before read | revalidate/deny |
| Chrome | normal specifically supported page editor | eligible only when browser context is supported and non-private |
| Chrome | Incognito | deny before content |
| Chrome | omnibox/browser chrome field | deny/unsupported |
| Chrome | login/payment page | deny relevant sensitive surfaces |
| Edge | normal specifically supported page editor | eligible only when supported and non-private |
| Edge | InPrivate | deny before content |
| Browser | same layout on different origin | direct Restore denied |
| Browser | privacy state cannot be established | deny |
| Logs | exception while handling draft | no raw draft text |
| Logs | UIA/platform diagnostic failure | no raw field values/tree dump |
| Storage | protected draft checkpoint | canary plaintext absent from storage |
| Storage | repeated typing in one draft | update current record, no revision-history growth |
| Retention | draft passes expiry while app runs | protected record deleted/unavailable |
| Retention | draft expires while app is stopped | startup cleanup deletes before presentation |
| Clear | user intentionally empties live field | obsolete prior text not resurrected |
| Completion | known successful send/save | obsolete draft removed |
| Focus | user switches away/back | not treated as completion |
| Recovery | strong fresh target match | Restore may be enabled after secure re-check |
| Recovery | target ambiguous | no blind/force restore; Preview/Copy safe fallback |
| Recovery | target changed to secure/private | direct Restore denied |
| Clipboard | Preview opened | clipboard unchanged |
| Clipboard | Copy chosen explicitly | clipboard written only by explicit action |
| Clipboard | Restore fails | no automatic Copy fallback |
| Delete | user discards draft | payload deleted, no hidden undo/history store |
