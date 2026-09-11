# Product Behavior Matrix

This table is the baseline product behavior future implementation should preserve.

| Situation | Observe metadata? | Read text? | Persist? | Show recovery? | Direct Restore? |
|---|---:|---:|---:|---:|---:|
| Supported ordinary safe field | Yes | After allow | Yes, protected | If lifecycle becomes recoverable | Only after strong fresh match |
| Password / protected field | Minimal metadata only | No where platform permits | No | No | No |
| Credential-like field | Minimal metadata only | No where platform permits | No | No | No |
| Banking/security-code field | Minimal metadata only | No where platform permits | No | No | No |
| Browser private/incognito | Enough to deny | No | No | No | No |
| Unsupported app/control | Enough to classify unsupported | No | No | No | No |
| Classification failure/timeout | Minimum attempted metadata | No | No | No | No |
| Safe draft, user switches focus | Yes | Continue as appropriate when field revisited | Keep current snapshot | Not merely because focus changed | N/A |
| Safe draft, user intentionally clears field | Yes | Yes (already allowed context) | Remove current draft after stable clear policy | No obsolete recovery | No |
| Safe draft, proven send/save | Yes | As required by profile | Remove obsolete draft | No | No |
| Safe draft, window/app unexpectedly closes | Context disappears | No further read | Keep latest protected snapshot | Yes | Later, after rematch |
| Recoverable draft expired | No need | No | Delete | Remove | No |
| Recoverable draft target missing | Current context check | No target write | Keep until expiry/discard | Yes | No; Preview/Copy remain |
| Similar but ambiguous target | Yes | No write | Keep | Yes | No |
| Target changed to secure field | Metadata to deny | No | Keep existing draft until normal lifecycle decision | Yes | No |
| User clicks Preview | No new target required | Decrypt locally for display | No new history | Preview | No mutation |
| User clicks Copy | No new target required | Decrypt locally | Keep draft | Yes | Clipboard only, explicit |
| User clicks Discard | No | No | Delete | Remove | No |
| Successful Restore | Fresh target metadata + safety | Decrypt for operation | Delete recoverable payload after success | Remove/refresh | Yes once |

## Rules encoded by the matrix

1. Metadata observation and text reading are distinct capabilities.
2. Denied/unknown contexts do not cross the content gate where avoidable.
3. Recovery visibility does not imply restore permission.
4. Restore always revalidates the live target.
5. Copy is explicit and is not equivalent to Restore or Discard.
6. Intentional clear/send/save should not leave obsolete recovery data.
