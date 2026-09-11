# UI Automation Cache and Property Budget Specification

## Objective

Minimize cross-process UI Automation calls and prevent accidental retrieval of content-bearing properties.

## Baseline cache request

For Phase 1 metadata experiments, prefer one shallow request containing only audited Tier A properties where the chosen UIA API allows it:

- ProcessId
- NativeWindowHandle
- ControlType
- FrameworkId
- IsPassword
- IsEnabled
- IsKeyboardFocusable
- HasKeyboardFocus
- IsOffscreen
- BoundingRectangle
- selected pattern-availability booleans if needed for target capability reconnaissance

Do **not** add Name, HelpText, ItemStatus, ValuePattern.Value, or TextPattern text.

## Tree scope

Start with the focused element only (`Element`) and explicitly bounded parent/top-level navigation if needed. Do not cache an entire desktop subtree.

## Property budget

A Phase 1 metadata job should have a small fixed maximum count of cross-process property/pattern acquisitions. Any new property requires:

1. entry in `UIA_PROPERTY_SAFETY_CLASSIFICATION.md`;
2. target/experiment justification;
3. performance measurement;
4. privacy review;
5. test coverage.

## Time budget

Every provider-bound operation is cancellable at the orchestration level and has a bounded timeout policy. Because cancellation cannot necessarily abort an in-flight provider COM call, the dedicated worker must isolate UI hangs from the Avalonia UI thread and from shutdown.

## Cache lifetime

Cached metadata is tied to one observation/reconciliation result. Do not treat cached UIA properties as authoritative across a context-generation change.

## Diagnostics

Allowed measurements:

- property requested by enum/id;
- success/timeout/failure count;
- duration bucket;
- provider/framework enum/token;
- cache hit/miss where meaningful.

Forbidden diagnostics:

- raw property string values from Tier B/C/D;
- element Name/HelpText/value;
- full accessibility tree dumps.
