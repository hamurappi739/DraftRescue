# Detailed Phase Breakdown

This refines the canonical roadmap into small gates without changing its order.

## Phase 0 — Architecture

0.1 Repository/solution skeleton  
0.2 Dependency boundaries  
0.3 Privacy/security invariants  
0.4 Minimal Avalonia shell  
0.5 Core fail-closed policy tests  
0.6 Windows build/test/launch verification

## Phase 1 — Active App + Field Detection

1.1 Foreground application identity proof-of-concept  
1.2 Focused-element metadata proof-of-concept  
1.3 Normalize app/window/field metadata  
1.4 Notepad test target  
1.5 Lifecycle/cancellation/resource measurements  
1.6 No text persistence; no broad app support

## Phase 2 — Secure Field Guard

2.1 Define detector signals  
2.2 Native password-field cases  
2.3 Unknown/failure cases fail closed  
2.4 Privacy regression tests  
2.5 Ensure content is not read before guard where platform allows

## Phase 3 — Draft Tracking Prototype

3.1 One explicitly supported safe target  
3.2 Current draft state only, no history  
3.3 Update/coalescing behavior  
3.4 Close/reopen experiment  
3.5 Memory/resource checks

## Phase 4 — Encrypted Local Persistence

4.1 Storage choice ADR  
4.2 protection/encryption design  
4.3 protected record write/read  
4.4 retention timestamps  
4.5 restart/crash consistency  
4.6 plaintext/logging audit

## Phase 5 — Recovery UI

5.1 recoverable-draft list/state  
5.2 Preview  
5.3 Discard  
5.4 Copy only after clipboard review

## Phase 6 — Restore

6.1 target matching confidence  
6.2 safe restore for first target  
6.3 mismatch/failure behavior  
6.4 restore-state lifecycle

## Phase 7 — Browser Support

Chrome first, then Edge. Browser private modes must be denied by default before browser draft capture is accepted.

## Phase 8 — Electron Apps

Start with Discord; Telegram Desktop remains later according to canonical priorities.

## Phase 9 — App Profiles

Isolate per-app rules, exceptions, and capabilities.

## Phase 10 — Hardening & Privacy Tests

Cross-app privacy matrix, secure fields, retention, crash recovery, resource usage, restore ambiguity, logging audit, packaging behavior.
