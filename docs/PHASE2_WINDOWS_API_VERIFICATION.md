# Phase 2 Windows API Verification Notes

**Purpose:** record authoritative platform facts that shape the design. These are research notes, not proof that a target is supported.

## `IsPassword`

Microsoft documents `AutomationElement.AutomationElementInformation.IsPassword` as a boolean indicating whether the UI Automation element contains protected content. DraftRescue therefore treats `true` as a hard deny. `false` is deliberately **not** treated as proof of safety.

Source:
https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.automationelement.automationelementinformation.ispassword?view=windowsdesktop-10.0

## UI Automation caching

Microsoft documents that clients explicitly choose properties/patterns to cache. A cached pattern does not automatically cache its properties. It also documents `AutomationElementMode.None`, which can return cache-only references that cannot access uncached current properties/patterns.

DraftRescue uses this as defense-in-depth for metadata-only acquisition: request only audited Tier A/B properties and avoid opportunistic current-property calls.

Sources:
https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-cachingforclients
https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/caching-in-ui-automation-clients

## Stale elements

Microsoft documents `ElementNotAvailableException` for UI Automation elements whose corresponding UI is no longer available. DraftRescue maps this to stale/unavailable deny, never to a fallback read.

Source:
https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.elementnotavailableexception?view=windowsdesktop-10.0

## Product policy beyond platform guarantees

The following are DraftRescue decisions, not Microsoft guarantees:

- `IsPassword=false` is insufficient to allow;
- credential/payment surfaces are denied more broadly than protected controls;
- every target-content read requires fresh one-read capability;
- capability expires/revokes on generation/binding changes;
- generic browser fields are not allowed in Phase 2.
