# Current Project Mode

**Status:** user-approved override to the original execution workflow.

## Current mode

DraftRescue is being prepared **from scratch without Cursor for now**.

ChatGPT is responsible for producing the deepest practical pre-implementation package before a coding agent is introduced, including:

- product requirements;
- UX/UI behavior;
- architecture and ADRs;
- module boundaries;
- domain/state models;
- interfaces/contracts;
- Windows/UI Automation research plans;
- privacy/security invariants;
- persistence/crypto design;
- recovery/restore rules;
- app-profile format;
- test scenarios and certification gates;
- repository structure and safe skeleton code where useful.

## Future Cursor role

Cursor is introduced only when the user explicitly decides the handoff package is ready.

At that point Cursor should primarily implement already-defined work packages and run/fix builds/tests. It must not reinterpret the project from scratch or silently redesign accepted decisions.

## Language rule

When future Cursor prompts are eventually requested, the prompts are written in English. Normal collaboration with the user remains in Russian.
