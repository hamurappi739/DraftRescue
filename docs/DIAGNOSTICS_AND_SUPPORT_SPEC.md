# Diagnostics & Support Specification

## 1. Principle

DraftRescue diagnostics must help debug the system **without becoming a second copy of user content**.

## 2. Safe event examples

Allowed structured fields may include:

- timestamp;
- event category;
- component name;
- opaque DraftId/session correlation ID;
- app profile ID;
- allow/deny reason enum;
- operation duration;
- count of recoverable drafts;
- storage error category;
- restore capability enum.

## 3. Forbidden diagnostic fields

Never log:

- draft text or preview;
- clipboard content;
- passwords/PIN/OTP/security codes;
- field Value/TextPattern ranges;
- raw URLs with query/fragment;
- raw window/page titles unless an explicit safe-minimization rule exists;
- accessibility tree dumps from real user contexts;
- encrypted payload bytes as a substitute for “safe logging.”

## 4. Exception handling

Raw exception messages from platform/accessibility providers can occasionally include UI text or context. At module boundaries, map failures to safe categories for normal logs/UI.

Detailed developer diagnostics should still avoid dumping automation elements/property bags.

## 5. Support bundle

If a future support-bundle feature exists, default bundle contents may include:

- app version;
- OS/runtime versions;
- enabled app-profile IDs;
- safe configuration values such as retention duration;
- sanitized structured logs;
- feature capability/error counters.

It must exclude the draft database, encrypted payloads, previews, clipboard, and accessibility dumps.

Bundle creation must be explicit user action and remain local until the user deliberately chooses how to share it.

## 6. No telemetry by default

The canonical product is local/privacy-first. Do not add analytics, crash upload, remote logging, or telemetry as an incidental dependency.

Any future remote diagnostic feature requires an explicit product decision, privacy review, disclosure, and opt-in design.
