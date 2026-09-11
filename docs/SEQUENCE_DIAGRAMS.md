# Critical Sequence Diagrams

These diagrams are normative ordering constraints, not implementation-threading prescriptions.

## 1. Observe and checkpoint an allowed draft

```mermaid
sequenceDiagram
    participant OS as Windows events
    participant Obs as Foreground/Focus Observer
    participant Loc as Candidate Field Locator
    participant Guard as SecureInputGuard
    participant Read as EligibleFieldTextReader
    participant Track as DraftTracker
    participant Prot as DraftProtector
    participant Repo as DraftRepository

    OS->>Obs: foreground/focus changed
    Obs->>Loc: locate metadata-only candidate
    Loc-->>Obs: CandidateFieldMetadata
    Obs->>Guard: evaluate(metadata)
    Guard-->>Obs: Allowed policy result
    Obs->>Guard: issue current one-read capability
    Guard-->>Obs: AllowedFieldHandle
    Obs->>Read: atomic claim + read(AllowedFieldHandle)
    Read-->>Obs: FieldTextSnapshot
    Obs->>Track: apply allowed snapshot
    Track->>Prot: protect current draft snapshot
    Prot-->>Track: ProtectedDraftPayload
    Track->>Repo: upsert current record
    Repo-->>Track: committed
```

**Forbidden ordering:** Read text -> classify security.

## 2. Denied/uncertain field

```mermaid
sequenceDiagram
    participant Loc as Candidate Field Locator
    participant Guard as SecureInputGuard
    participant Read as Text Reader
    participant Repo as DraftRepository

    Loc->>Guard: metadata only
    Guard-->>Loc: DeniedSecure / DeniedPrivate / DeniedUncertain
    Note over Read: MUST NOT be called
    Note over Repo: MUST NOT receive a new payload
```

## 3. Context loss without completion

```mermaid
sequenceDiagram
    participant Obs as Observer
    participant Track as DraftTracker
    participant Repo as Repository

    Obs->>Track: context lost
    Track->>Track: classify reason
    alt strong completion/clear evidence
        Track->>Repo: delete/transition according to completion policy
    else crash/window/tab/focus loss or unknown
        Track->>Repo: keep recoverable current snapshot until retention expiry
    end
```

Focus loss alone is never strong completion evidence.

## 4. Startup and recovery list

```mermaid
sequenceDiagram
    participant App as Desktop startup
    participant Ret as RetentionService
    participant Repo as Repository
    participant UI as Recovery UI
    participant Prot as DraftProtector

    App->>Ret: purge expired
    Ret->>Repo: delete expired records
    App->>Repo: list recoverable metadata
    Repo-->>App: metadata only
    App-->>UI: cards without decrypted preview
    Note over Prot: no mass decryption at startup
```

## 5. Preview

```mermaid
sequenceDiagram
    participant UI as UI
    participant Q as PreviewDraft use case
    participant Repo as Repository
    participant Prot as DraftProtector

    UI->>Q: preview(draftId)
    Q->>Repo: get protected record
    Repo-->>Q: protected payload
    Q->>Prot: unprotect
    Prot-->>Q: transient plaintext
    Q-->>UI: transient preview model
```

UI must not cache plaintext preview indefinitely.

## 6. Direct Restore

```mermaid
sequenceDiagram
    participant UI as UI
    participant RS as RestoreService
    participant Repo as Repository
    participant Loc as Live Target Locator
    participant Guard as SecureInputGuard
    participant Match as RecoveryMatcher
    participant Writer as Restore Adapter

    UI->>RS: restore(draftId)
    RS->>Repo: get protected metadata/payload
    RS->>Loc: find current target
    Loc-->>RS: metadata-only live target
    RS->>Guard: fresh security evaluation
    Guard-->>RS: eligibility
    alt not Allowed
        RS-->>UI: restore unavailable
    else Allowed
        RS->>Match: match stored vs live evidence
        Match-->>RS: Strong / Ambiguous / NoMatch
        alt Strong
            RS->>Writer: write after target revalidation
            Writer-->>RS: success
            RS->>Repo: remove/mark restored
            RS-->>UI: restored
        else Ambiguous or NoMatch
            RS-->>UI: Preview/Copy only
        end
    end
```

## 7. Copy

```mermaid
sequenceDiagram
    participant UI as UI
    participant Copy as CopyDraft use case
    participant Prot as DraftProtector
    participant Clip as Clipboard adapter

    UI->>Copy: copy(draftId)
    Copy->>Prot: unprotect protected payload
    Prot-->>Copy: transient plaintext
    Copy->>Clip: set clipboard
    Copy-->>UI: copied
```

Clipboard is an explicit user action and a known leakage boundary. Automatic clipboard clearing is not assumed.

## 8. Expiry race with UI action

```mermaid
sequenceDiagram
    participant UI as UI
    participant Cmd as Command handler
    participant Repo as Repository

    UI->>Cmd: preview/copy/restore(draftId)
    Cmd->>Repo: fetch current record
    alt missing or expired now
        Cmd-->>UI: draft expired/unavailable
    else recoverable
        Cmd->>Cmd: continue operation
    end
```

The UI card's previously displayed expiry state is not authorization.
