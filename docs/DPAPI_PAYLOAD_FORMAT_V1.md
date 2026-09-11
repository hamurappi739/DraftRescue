# DPAPI Protected Payload Format v1

## Goal

Define the exact bytes presented to Windows DPAPI and the exact opaque bytes accepted by persistence.

## Plaintext payload before protection

Canonical logical structure:

```text
DraftPayloadV1
  magic               = "DRP1"            (4 ASCII bytes)
  payload_version     = 1                  (uint16 LE)
  text_encoding       = UTF8               (uint8 = 1)
  reserved            = 0                  (uint8)
  text_byte_length    = uint32 LE
  text_utf8           = exact UTF-8 bytes
```

No URL/title/label/match metadata is duplicated inside the body.

## Protection operation

Conceptually:

```text
plaintextBytes = Encode(DraftPayloadV1)
protectedBytes = ProtectedData.Protect(
    plaintextBytes,
    optionalEntropy: empty,
    scope: CurrentUser)
```

For v1, optional entropy is empty. Do not invent machine IDs, usernames, SID strings, registry values, or app paths as entropy.

## Stored representation

SQLite stores:

```text
protection_version = 1
protected_payload  = DPAPI output bytes
```

The DPAPI blob is opaque. Application code must not parse or partially mutate it.

## Unprotect

Unknown `protection_version` fails before calling an unrelated decoder. Successful DPAPI unprotect is followed by strict payload validation: magic/version/length/UTF-8 validity.

Malformed plaintext envelope after successful unprotect is `InvalidProtectedPayload`, not a best-effort string decode.

## Memory hygiene

- use short-lived byte buffers;
- clear mutable UTF-8 plaintext byte arrays in `finally` where practical;
- avoid logging sizes that become identifying at high precision unless needed; bucket when diagnostics suffice;
- managed `string` secure erasure is not promised.

## Threat boundary

`CurrentUser` means the protected data is associated with the current Windows user; code running under that same user context is not outside the threat boundary. DraftRescue must not claim resistance to malware already executing as the user.
