# Positive Allow Predicates Specification

## Principle

DraftRescue does not ask "does this field look non-dangerous?". It asks:

> "Does this field exactly match a product-owned, version-certified ordinary text surface whose required negative safety checks are all known and clear?"

## Predicate ownership

Positive allow predicates belong to an `AppProfile`/target adapter pair and are release-bound. A remote server may not broaden them in MVP.

## Allowed predicate inputs

Only audited structural signals may participate:

- application executable/product identity;
- certified app version range;
- control type;
- framework ID;
- read-only/editability state;
- protected-content state;
- pattern availability booleans;
- narrowly reviewed AutomationId/ClassName where the target certification demonstrates stability;
- bounded structural ancestry/relationship tokens if explicitly approved for that profile;
- target adapter's own product-owned surface kind enum.

## Forbidden generic predicate inputs

Do not use these as generic positive evidence:

- typed text;
- `Name`/label substring dictionaries;
- full window title;
- full URL/path/query;
- "not a password" alone;
- "is editable" alone;
- geometry alone;
- first match in accessibility tree.

## Synthetic v1 allow predicate

The test harness may define one deterministic ordinary field:

```text
AppProfileId == "draftrescue.synthetic"
ProfileVersion == certified synthetic version
ControlType == Edit
Editable == true
ReadOnly == false
ProtectedContent == false
ProviderHealth == Healthy
BindingStatus == Current
SyntheticSurfaceKind == OrdinaryDraftField
```

All conditions are conjunctive. Any unknown fails closed.

## Notepad

Notepad Phase 2 may establish a **candidate** positive predicate from controlled reconnaissance, but it remains Experimental until the complete target certification sequence is passed. Phase 2 must not globally declare all Notepad editable surfaces safe.

## Browser rule

No generic browser web-form positive allow predicate is permitted in Phase 2. Browser form capture waits for Phase 7 private-mode and sensitive-purpose certification.

## Version invalidation

A predicate is valid only inside the profile's certified target-version range. Unknown/new versions return unsupported, not nearest-match.
