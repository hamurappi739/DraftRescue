# Safe Presentation Metadata Specification

**Status:** accepted MVP rule for body-free Recovery-list rendering.

## 1. Problem

DraftRescue minimizes persisted raw context. A keyed browser-origin/window/field fingerprint can be compared later, but it cannot be converted back into a human-readable domain/title. Persisting raw page title, full URL, field label, or accessibility name merely to make a prettier list would undermine metadata minimization.

Therefore the MVP Recovery list does **not** promise dynamic site/window/field names after restart.

## 2. Allowed list metadata

A recoverable-card summary may contain:

```text
DraftId
ApplicationId -> static ApplicationDisplayName/icon via local signed profile/app identity
DraftPresentationKind
UpdatedAtUtc
ExpiresAtUtc
RestoreAvailability
PreviewAvailability
CopyAvailability
```

Canonical coarse `DraftPresentationKind` values:

```text
Unknown
GenericText
MessageComposer
WebForm
Comment
Document
Note
```

The enum/category contains no user-entered text and no raw site/window/field name.

## 3. Not persisted for ordinary list display

Do not persist plaintext solely for display:

- website/domain/origin string;
- full URL;
- page/window title;
- conversation/channel/contact name;
- document filename if it can contain user information;
- accessibility Name/HelpText;
- field label/placeholder;
- draft-body snippet.

A later product version may add an **encrypted, explicitly reviewed** presentation envelope if usability proves inadequate, but that is outside MVP and requires an ADR/privacy review.

## 4. Example cards

```text
Google Chrome
Web form
Updated 2 min ago · Expires in 28 min
[Preview] [Copy] [Restore]
```

```text
Notepad
Document
Updated 8 min ago · Expires in 22 min
[Preview] [Copy]
```

If several drafts have identical application/kind, recency is the ordinary discriminator. UI may number currently displayed equal cards (`Chrome draft 1`, `Chrome draft 2`) **ephemerally** without persisting that ordinal.

## 5. Preview

Explicit Preview reveals the selected draft body, not additional secretly persisted browsing metadata. The product may show static application/profile information alongside it.

## 6. Matching remains richer than display

Recovery matching may use non-reversible keyed fingerprints and safe enum metadata that are intentionally not human-readable. The UI must not request raw context merely because matching has opaque evidence.
