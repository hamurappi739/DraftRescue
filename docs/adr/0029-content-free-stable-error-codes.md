# ADR 0029 — Stable content-free error taxonomy

**Status:** Accepted

## Decision

Expected operational failures use stable `DR-*` codes and audited structural fields. Raw provider/exception messages are not blindly logged because they may contain user-derived content.
