# Snapshot Timing Experiment Plan

**Applies later to Phase 3.** Phase 1 must not read text. This plan defines how to choose timing constants rather than guessing them.

## Goal

Balance three competing requirements:

1. recover recent text after crash/reboot;
2. avoid durable write per keystroke;
3. keep CPU/disk/DPAPI overhead low.

## Candidate scheduler model

A dirty draft schedules a trailing checkpoint. A separate maximum-dirty-age ceiling ensures a long continuous typing burst still receives periodic durable checkpoints.

Variables to measure:

- `TrailingDebounceMs`
- `MaxDirtyAgeMs`
- minimum interval between durable commits;
- maximum pending snapshot count (should coalesce to latest);
- retry delay after transient protection/storage failure.

## Experimental ranges

Do not lock until measured. Suggested candidates:

| Parameter | candidates |
|---|---|
| trailing debounce | 300, 500, 750, 1000, 1500 ms |
| max dirty age | 3, 5, 10, 15 s |
| minimum commit interval | 500 ms, 1 s, 2 s |

## Workloads

- 20-character short message;
- 500-character steady typing;
- 5-minute continuous fast typing;
- paste of 20 KB synthetic text;
- repeated edits/backspace;
- focus switch immediately after typing;
- process kill at randomized intervals;
- storage busy for 1–10 s;
- laptop-like slow disk simulation where practical.

## Metrics

- worst-case synthetic text loss window after kill;
- commits/minute;
- protected bytes/minute;
- CPU time;
- DB transaction duration;
- number of stale/coalesced snapshots;
- recovery freshness distribution.

## Selection rule

Choose a configuration that satisfies explicit resource budgets and a documented target loss window. Do not optimize solely for minimum disk writes or minimum theoretical text loss.

## Privacy rule

The experiment uses generated synthetic text. No real user draft corpus is needed.
