# Profile Resolution Test Matrix

| ID | Input | Expected |
|---|---|---|
| PRF-001 | exact supported app identity + certified version | Supported(profile) |
| PRF-002 | exact identity + newer unknown version | UnsupportedVersion |
| PRF-003 | explicit blocked version | ExplicitlyBlocked |
| PRF-004 | no profile | UnsupportedNoProfile |
| PRF-005 | experimental profile in production mode | not enabled |
| PRF-006 | experimental profile in explicit dev/test mode | Experimental(profile) |
| PRF-007 | two eligible supported profiles overlap | AmbiguousProfiles + build/release validation failure |
| PRF-008 | schema-invalid manifest | InvalidProfile |
| PRF-009 | DraftRescue own process | unsupported/ignored |
| PRF-010 | unsupported integrity boundary | unsupported before content read |
| PRF-011 | browser family matches but executable identity does not | no browser profile match |
| PRF-012 | persisted old profile version + current compatible newer profile | restore still requires fresh classification/evidence; no automatic authorization |
| PRF-013 | target version parse failure | UnsupportedVersion/Unknown, never nearest-match |
| PRF-014 | manifest file order shuffled | same resolution result |
