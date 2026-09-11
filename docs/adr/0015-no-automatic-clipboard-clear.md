# ADR 0015 — No Automatic Clipboard Clear in MVP

**Status:** Accepted

## Decision

Copy writes plaintext to the clipboard only on explicit user action. DraftRescue does not automatically clear the clipboard afterward.

## Why

A delayed clear can destroy newer unrelated clipboard contents and create surprising behavior. Clipboard history/sync is an OS/user boundary that DraftRescue cannot safely control generically.
