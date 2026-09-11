# ADR 0004: Manual composition in Phase 0

**Status:** Accepted for Phase 0

## Decision

Do not add a dependency-injection container in Phase 0.

## Why

There are no runtime services to compose yet. Manual construction keeps the architecture visible and avoids framework dependency before it provides value.

## Revisit when

The application has several concrete adapters/services with lifecycle, options, or disposal requirements that make manual composition error-prone.
