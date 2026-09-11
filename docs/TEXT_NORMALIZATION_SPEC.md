# Text Normalization Specification

## Canonical decision

DraftRescue preserves user text **exactly as returned by the certified target adapter**. The generic pipeline performs no semantic or cosmetic normalization.

Forbidden generic transformations:

- `Trim()` / whitespace collapse;
- newline conversion;
- Unicode NFC/NFD/NFKC/NFKD normalization;
- smart quote replacement;
- case folding;
- spell correction;
- zero-width character removal;
- HTML/Markdown parsing;
- line-ending rewriting.

## Provider artifact exception

A target adapter may define a narrow deterministic artifact-removal rule only if certification demonstrates that the provider itself injects a non-user artifact. Such a rule must be profile-version-bound, documented, fixture-tested, and must not infer semantics.

## Equality

Current-snapshot equality is ordinal exact string equality. A semantically equivalent but byte/UTF-16-different value is a different snapshot.
