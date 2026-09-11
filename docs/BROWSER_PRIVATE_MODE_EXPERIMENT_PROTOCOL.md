# Browser Private-Mode Experiment Protocol

**Status:** mandatory research protocol before Chrome/Edge/Firefox can move from experimental to supported.

## 1. Safety rule

The purpose is not to find a clever heuristic that “usually” identifies private mode. The purpose is to prove that the exact detector/profile/version combination fails closed **before text read**.

A detector outcome is only:

```text
NormalConfirmed
PrivateConfirmed
Unknown
```

`PrivateConfirmed` and `Unknown` both block text read.

## 2. Instrumentation

Use the synthetic DraftRescue test harness with:

- metadata-only browser detector tracing;
- a spy `IEligibleFieldTextReader` invocation counter;
- synthetic ordinary/credential/payment page fixtures;
- no real user accounts, passwords, forms, history, or browsing data.

The pass criterion for a denied private scenario is not merely “nothing persisted”; it is **reader invocation count = 0**.

## 3. Test dimensions

For each browser family/profile candidate, record:

- DraftRescue build;
- Windows edition/build;
- browser exact version;
- browser architecture;
- profile version;
- detector version;
- display scale(s);
- normal/private coexistence state;
- browser profile/account state using synthetic local profiles only.

## 4. Required scenarios

### Window lifecycle

- normal window only;
- private window only;
- normal + private simultaneously;
- create private after normal is already classified;
- create normal after private;
- close/reopen either window;
- minimize/restore;
- move tab between windows where browser supports it;
- browser restart with surviving/reopened tabs;
- browser update between runs.

### Surface coverage

- ordinary `<input type=text>`;
- textarea;
- contenteditable/rich editor fixture;
- password input;
- synthetic PIN/security-code field;
- synthetic payment/card field;
- address bar/browser chrome;
- DevTools;
- browser settings/internal pages;
- extension surface if reachable in test environment.

### Multi-context

- two normal windows;
- two private windows;
- normal and private windows on different monitors;
- tab/window focus churn at high frequency;
- accessibility tree recreation.

## 5. Candidate signal policy

Candidate detector signals may include metadata exposed by the browser/window/accessibility/provider environment. During research they may be combined, but no candidate signal is accepted merely because it is visually obvious or title-based.

The final supported detector must document:

- which signals are required;
- which conflicts force `Unknown`;
- detector cache key and TTL;
- invalidation conditions;
- exact certified browser version range.

No raw URL/title values are written into normal diagnostics.

## 6. Certification threshold

For the exact certified matrix:

- **zero** observed private->normal false negatives;
- all `Unknown` cases deny before read;
- normal/private coexistence classified per window/context, not process-wide;
- browser chrome/credential/payment fixtures denied;
- detector remains bounded under focus churn;
- no raw field content appears in research logs.

A single private false-negative blocks `supported` status for that detector/profile version until the cause is understood and fixed.

This is a certification threshold for tested builds, not a claim of mathematical proof for all future browser versions. Unknown future versions fail closed.

## 7. Artifact produced by experiment

Each certification run produces a content-free report:

```text
BrowserPrivacyCertification
- browser family/version
- Windows versions/builds
- profile/detector versions
- scenario IDs
- result per scenario
- reader invocation counts in deny tests
- timing/timeout observations
- known limitations
- certification decision
```

No screenshots containing real user data and no copied browser profile directories are retained.
