# Recovery UX Specification

Status: baseline recovery behavior.  
Detailed visual/layout specification: [`UI_UX_MASTER_SPEC.md`](UI_UX_MASTER_SPEC.md)  
Presentation state contract: [`UI_STATE_MODEL.md`](UI_STATE_MODEL.md)  
Initial wording: [`UI_COPY_BASELINE.md`](UI_COPY_BASELINE.md)

## Normal operation

DraftRescue should be almost invisible during ordinary typing. Do not show a popup on every character or routine draft update.

The product is a recovery utility, not a typing-history application.

## When a recoverable draft exists

The canonical actions are:

- Restore
- Preview
- Copy
- Discard

## Safety behavior

- Restore should be offered only when the intended target can be matched safely enough for the current implementation.
- Preview must not alter the target field.
- Copy is explicitly user-triggered and must be treated as a clipboard-leakage risk in implementation.
- Discard removes the recoverable draft.
- Expired drafts disappear automatically according to retention.
- No "restore anyway" override should bypass an unsafe target match in MVP.

## Early MVP

The early UI may be conservative. It is acceptable for some cases to offer Preview/Copy without automatic Restore when matching confidence is insufficient.

The preferred UI model is:

**Quiet background utility + small Recovery inbox + compact Settings page.**
