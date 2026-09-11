# ADR 0023 — Recovery list is metadata-only

**Status:** Accepted

## Decision

The MVP Recovery list does not automatically decrypt draft bodies or render text snippets. Draft plaintext is revealed only after explicit Preview, Copy, or authorized Restore.

## Rationale

This reduces plaintext lifetime, avoids decrypting all drafts at startup/list render, keeps UI simpler, and supports least privilege.

## Consequence

Cards may be less descriptive; approved application/context labels and timestamps carry the list UI.
