# ADR 0044 — Security Unknown Is Not False

**Status:** Accepted

## Decision

Security signal states distinguish known false from unknown/unavailable/failed. Required unknown states deny.

## Why

Collapsing provider failure or missing data into `false` can accidentally create permission.
