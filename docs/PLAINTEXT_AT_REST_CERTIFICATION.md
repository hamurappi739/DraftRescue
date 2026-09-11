# Plaintext-at-Rest Certification Protocol

## Release gate

Phase 4 cannot pass until a canary-based test demonstrates that known plaintext draft markers do not appear in DraftRescue-owned durable artifacts.

## Canary

Use synthetic high-entropy markers, e.g. generated per test:

```text
DR_TEST_CANARY_<random-guid>_END
```

Never use real user content.

## Test procedure

1. start with isolated fresh app-data directory;
2. create authorized synthetic draft containing one or more canaries;
3. checkpoint repeatedly with changed values;
4. force normal app shutdown/restart;
5. verify recovery metadata exists;
6. scan DraftRescue-owned files bytewise for UTF-8 and UTF-16 encodings of every canary;
7. update draft many times;
8. discard/expire it;
9. scan database, rollback journal if present, temporary files, settings, secret file, logs, crash/test output;
10. repeat around injected crash boundaries.

## Pass

- no plaintext canary in DB/journal/temp/log/settings/secret files;
- protected blob bytes do not equal plaintext bytes;
- metadata-only listing does not invoke Unprotect;
- no test output prints the canary.

## Important limitation

This certifies DraftRescue-owned application artifacts under the tested environment. It does not prove that operating-system paging, crash dumps, antivirus, filesystem snapshots, SSD firmware, or unrelated system components never retain memory/disk data.

## Regression rule

Any change to serializer, protector, repository, journal mode, logging, crash handling, or diagnostics reruns this certification suite.
