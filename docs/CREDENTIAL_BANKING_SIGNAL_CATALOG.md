# Credential, Security-Code, and Banking Signal Catalog

**Status:** policy catalog for Phase 2+ design. This is not permission to scrape field labels.

## Policy

DraftRescue must not attempt to recover credentials, PINs, one-time codes, card/bank details, authentication secrets, or equivalent sensitive inputs.

## Signal classes

### S1 — trusted hard signals

Examples:

- UIA `IsPassword == true`;
- OS/provider protected-content semantics explicitly certified for a target;
- target profile marks the entire surface/class as sensitive.

Effect: immediate deny.

### S2 — structural target/profile signals

Examples that may be used only when stable and certified:

- known AutomationId token for password/PIN/payment field;
- known class/control hierarchy for credential dialogs;
- known target adapter state indicating authentication/payment mode;
- explicit browser adapter semantic classification if a future certified adapter can obtain it without reading typed value.

Effect: deny when matched.

### S3 — free-form textual labels/placeholders

Examples: `Password`, `CVV`, `Card number`, `OTP`, localized variants.

Generic UIA scraping of Name/HelpText/labels to build a broad semantic classifier is **not** the MVP baseline because those properties can themselves leak user/context strings and are incomplete/localized.

Effect: do not make this the foundation of safety. If a future adapter uses a bounded label allow/deny list, it requires a dedicated ADR, privacy review, language tests, and proof that the label source cannot be typed content.

### S4 — unknown editable surface

Unknown does not mean safe.

Effect: deny unless a positive certified allow predicate exists.

## Browser consequence

Generic browser form capture is not allowed in early phases. Browser support must prove private-mode detection and sensitive-purpose exclusion per certified browser/version before any text read.

## Test fixtures

Use synthetic obvious values only, such as:

- `TEST_PASSWORD_DO_NOT_CAPTURE`
- `4111111111111111` only as an industry test number in isolated synthetic harness;
- `000000` synthetic PIN/OTP;
- fake IBAN-like strings explicitly marked synthetic.

Never import real browser/autofill data into fixtures.
