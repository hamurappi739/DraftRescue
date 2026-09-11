# ADR 0008 — Keyed Context Fingerprints

**Status:** Accepted

## Decision

User-derived correlation metadata is minimized and, where equality matching is sufficient, represented by HMAC-SHA-256 tokens keyed with a random installation-local key protected by DPAPI CurrentUser. Ordinary stored tokens are truncated to 128 bits and versioned.

## Why

Plain hashes of low-entropy titles/domains/labels are dictionary-testable. Raw strings enlarge privacy exposure. Keyed tokens preserve equality correlation without storing source values.

## Consequences

- key loss/reset invalidates matching metadata;
- existing drafts are discarded before deliberate key replacement;
- matcher stores individual evidence tokens rather than one composite hash.
