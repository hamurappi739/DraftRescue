# Observation & Privacy-Gating Pipeline

**Design status:** proposed architecture derived from the canonical privacy requirements. Runtime implementation starts only in later approved phases.

## Core rule

Whenever the platform permits it, classify the field/context as safe **before reading the field's text value**.

```text
1. Foreground application identity
2. Candidate focused/editable field metadata
3. Browser/private-context metadata where available
4. SecureInputGuard decision
       | deny / unknown
       +----------------------> DROP; do not read text
       |
       | explicit allow
       v
5. Read the minimum current draft snapshot needed
6. DraftTracker
7. Protection/encryption boundary
8. Local protected persistence
9. Retention expiry/deletion
```

This order intentionally avoids a design where secure text is read globally and filtered only afterwards.

## InputObservation responsibility

Future observation code should report supported candidate editable contexts and changes needed for draft recovery. It must not become a reusable stream of every system keystroke.

The observation mechanism is not decided in Phase 0. Exact Windows APIs must be validated during Phase 1/2.

## ContextDetection responsibility

Normalize only the minimum context needed to identify:

- application;
- window context;
- field context;
- signals needed by privacy gating and later recovery matching.

Raw window titles, URLs, accessibility values, or other strings may themselves contain sensitive/user data. Prefer derived/normalized/opaque identifiers when they can satisfy the same purpose.

## SecureInputGuard responsibility

Return an explicit `CaptureEligibility` decision. Only `Allowed` permits later persistence. Secure, credential-like, banking, private-browsing, unsupported, and uncertain states fail closed.

## Content read boundary

The first component that may obtain actual draft text should exist **after** the guard in the logical pipeline. When an OS API forces text and metadata to arrive together, that adapter becomes a higher-risk boundary and must guarantee immediate discard for denied/uncertain contexts without logging or persistence.
