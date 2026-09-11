# ADR 0012 — Context Generation for Race Control

**Status:** Accepted

## Decision

Foreground/focused target changes increment an in-process context generation. Results from metadata/security/read operations are accepted only if their generation is still current.

## Why

Cross-process accessibility calls can be slow. Without generation checks, a late result could attach text or permission from one field to another.

## Consequences

- stale async results are discarded;
- restore uses its own fresh target generation/revalidation;
- platform/application contracts carry opaque generation tokens.
