# Phase 2 Synthetic Positive-Allow Profile

## Goal

Prove the positive-permission pipeline without claiming a real application is safe.

## Product-owned profile

Use only the existing synthetic profile identity (`draftrescue.synthetic` or equivalent canonical fixture id). Its version range is controlled by the repository and cannot drift underneath tests.

## Ordinary allow surface

Exactly one fixture surface kind is positive in Phase 2: `OrdinaryDraftField`.

Required conjunction:

```text
resolved synthetic profile
certified fixture version
current binding + generation
supported ordinary Edit/Document test control family
editable = known true
read-only = known false
protected-content = known false
provider healthy
sensitive purpose = None
positive structural fixture marker = matched
```

The marker must be product-owned/static fixture metadata, not the entered text.

## Negative siblings

The same fixture application contains protected, credential, PIN/OTP and payment surfaces. Their presence proves that trust is field-scoped, not process-scoped.

## Promotion rule

Passing the synthetic profile never promotes Notepad/Chrome/Edge to Supported. Real targets need independent certification.
