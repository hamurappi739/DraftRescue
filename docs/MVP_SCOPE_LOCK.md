# MVP Scope Lock

## Product promise

DraftRescue is a local Windows recovery layer for temporary unsaved text in a **small set of explicitly supported ordinary text fields**.

It is not a universal input recorder.

## In MVP

- background Windows desktop app;
- foreground app / focused field metadata observation;
- fail-closed secure/private gating;
- one current draft snapshot per eligible logical field/session;
- temporary encrypted local persistence;
- bounded retention;
- recovery list;
- Preview;
- Discard;
- Copy after clipboard review;
- direct Restore only for strongly matched supported targets;
- Notepad/control harness as initial validation target;
- Chrome then Edge after native target is proven;
- Discord later in Electron phase.

## Explicitly out of MVP

- full typing history;
- keyboard macro/keylogger functionality;
- cloud sync;
- account/login system;
- server backend;
- AI/semantic analysis;
- mobile;
- team/shared drafts;
- browser extension unless a future explicit architecture decision requires one;
- Firefox support in initial browser phase;
- arbitrary all-app support;
- OCR/screenshot capture;
- recovery of files/documents already durably saved by their host application;
- automatic restore without user action;
- force restore to ambiguous targets;
- password/PIN/OTP/payment capture;
- private/incognito capture;
- forever retention;
- deleted-draft recycle bin/history;
- analytics/remote telemetry by default.

## Scope expansion rule

A feature enters scope only if the user explicitly approves it and its privacy impact is reviewed. “It was easy to add” is not a reason.
