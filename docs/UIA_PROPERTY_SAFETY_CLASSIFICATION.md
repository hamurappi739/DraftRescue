# UI Automation Property Safety Classification

## Goal

Prevent a future implementation from treating every UI Automation property as harmless metadata. Some properties may contain user-controlled or sensitive strings.

## Tier A — structural, preferred before security decision

May be queried in the generic pre-classification path when supported and bounded:

- ProcessId
- NativeWindowHandle
- ControlType
- FrameworkId (normalized/tokenized)
- IsPassword
- IsReadOnly
- IsEnabled
- IsKeyboardFocusable
- HasKeyboardFocus
- IsOffscreen
- BoundingRectangle (transient/coarsened)
- supported-pattern availability flags

These properties still require exception/timeout handling.

## Tier B — structural but app/profile-gated

May be queried only when a profile or experiment explicitly justifies them:

- AutomationId
- ClassName
- runtime ID
- LabeledBy relationship identity (not label text)

Raw values are not automatically safe to persist or log. Unknown/free-form values should remain platform-local or be transformed into bounded tokens/fingerprints.

## Tier C — dynamic text-like metadata; generic pre-classification path forbidden

Do not generically read before an app/profile-specific policy proves necessity and safety:

- Name
- HelpText
- ItemStatus
- ItemType
- LocalizedControlType when a fixed enum can replace it
- text from LabeledBy element
- window/page title strings

Reasons: these can be provider-defined, localized, user-derived, or contain document/site/content context.

## Tier D — content; forbidden until AllowedFieldHandle exists

- ValuePattern.Value
- TextPattern document/range text
- LegacyIAccessible value/text
- selection text
- clipboard-derived text
- any equivalent provider-specific text/value property

Phase 1 must not access Tier D at all.

## IsPassword precedence

`IsPassword == true` is a hard deny signal. `false` is not a grant. `Unknown` fails closed when the active profile requires trusted protected-content classification.

## CacheRequest guidance

A cache request may batch Tier A properties to reduce cross-process calls. Do not include Tier C/D properties in a generic cache request simply for convenience, because caching itself retrieves them.

## Audit requirement

Every UIA property added to a cache request or metadata query must appear in this document or an approved target-profile extension, with its tier and reason.
