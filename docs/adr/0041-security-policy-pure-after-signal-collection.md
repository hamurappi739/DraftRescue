# ADR 0041 — Security Policy Is Pure After Signal Collection

**Status:** Accepted

## Decision

Windows/UIA adapters collect approved metadata into `SecurityEvidenceSet`; the allow/deny evaluator itself is deterministic and performs no I/O.

## Why

This makes deny precedence, permutations and unknown/failure behavior exhaustively testable without live UIA.
