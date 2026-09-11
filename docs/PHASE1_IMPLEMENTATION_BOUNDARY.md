# Phase 1 Implementation Boundary

This file exists so a future coding agent cannot interpret `Active App + Field Detection` as permission to capture text.

## Allowed production code in Phase 1

`DraftRescue.Platform.Windows`:

- foreground WinEvent subscription wrapper;
- UIA focus subscription wrapper;
- dedicated MTA worker/lifetime;
- bounded event queue/coalescer;
- focused-element metadata reader for audited Tier A/B properties;
- executable/package/version identity reader;
- normalized candidate mapper;
- typed provider failure mapping.

`DraftRescue.Application`:

- observation envelope/candidate contracts;
- context-generation orchestration;
- cancellation/stale-result handling;
- no content reader invocation.

`DraftRescue.Tests`:

- synthetic focus/event harness;
- table tests;
- callback content-free spies;
- timeout/backpressure/fault tests.

## Forbidden until later phase

- ValuePattern.Value read;
- TextPattern text read;
- any generic `GetText` abstraction wired to production observation;
- draft tracker;
- DPAPI/SQLite runtime storage;
- recovery UI data from actual drafts;
- Restore/copy implementation;
- browser capture enablement;
- credential label scraping;
- keyboard hooks/raw input.

## Phase 1 artifacts are disposable research evidence

Experimental target topology notes must not become implicit production support. A profile becomes supported only through the certification path.
