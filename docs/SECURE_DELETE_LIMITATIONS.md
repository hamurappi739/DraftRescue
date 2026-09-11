# Secure Delete / Physical Erasure Limitations

## Product promise

DraftRescue promises logical expiry/removal from its active recovery store. It does **not** promise forensic physical erasure from every storage layer.

## SQLite defense in depth

`PRAGMA secure_delete=ON` asks SQLite to overwrite deleted content within SQLite-managed pages. This reduces residual data inside the database file.

## What it cannot guarantee

Application-level deletion cannot reliably guarantee removal from:

- SSD wear-leveling/remapped NAND;
- filesystem snapshots/restore points;
- backup software;
- copy-on-write layers;
- storage controller caches;
- hibernation/pagefile/crash-dump artifacts;
- antivirus/indexing products;
- previously copied files.

## Design consequence

The strongest control is **never writing plaintext at rest in the first place**. Secure delete is secondary defense for ciphertext/fingerprint metadata and must not be marketed as anti-forensic wiping.

## No custom overwrite loop

MVP must not implement home-grown multi-pass overwrite algorithms. They add I/O and false confidence without solving modern SSD/filesystem behavior.
