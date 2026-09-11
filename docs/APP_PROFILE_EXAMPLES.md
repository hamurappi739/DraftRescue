# App Profile Examples

**Status:** illustrative experimental manifests, not declarations of support.

Machine-readable examples:

- `../specs/examples/app-profile.notepad-experimental.json`
- `../specs/examples/app-profile.chrome-experimental.json`
- `../specs/examples/app-profile.edge-experimental.json`
- `../specs/examples/app-profile.synthetic-supported.json` — synthetic-only schema fixture demonstrating certified target-version ranges.

The three real-target examples deliberately use `supportLevel: experimental`. The synthetic fixture is `supported` only to exercise schema/resolver rules and is not a declaration of support for a real application. Their purpose is to prove schema shape and conservative policy defaults before app-specific experiments.

## Notepad example

Uses an app-specific adapter placeholder because exact Notepad control capabilities vary by Windows version and must be measured. Save completion remains unproven.

## Chrome / Edge examples

Private mode is a hard deny requirement; browser-origin fingerprinting may be allowed only after the source origin is acquired through an approved adapter and transformed under the canonical keyed fingerprint policy. Generic submission semantics remain conservative. Direct Restore is initially `copyOnly` in the example manifests.

## Rule

Do not convert an example to `supported` by changing one JSON field. The certification template and exact version matrix must pass first.
