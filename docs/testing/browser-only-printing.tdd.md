# Browser printing fallback — historical TDD evidence

> Superseded on 13 July 2026 by `local-print-agent.tdd.md`. Browser printing remains supported as fallback, but it is no longer the only print path.

## User journey

As an operator, I want labels to print only through the browser so that the application has no Windows print agent, server spooler, or print-specific certificates to deploy.

## Evidence

| Guarantee | Test | Result |
| --- | --- | --- |
| The label page exposes `window.print()` and no server-print action | `WebPrintingOnlyTests.PrintClient_OffersBrowserPrintingOnly` | PASS |
| Production startup does not require `PrintAgent` settings | `WebPrintingOnlyTests.ProductionConfiguration_DoesNotRequirePrintAgentSettings` | PASS |
| No server print worker or spooler type is registered/present | `WebPrintingOnlyTests.Application_DoesNotRegisterServerPrintWorkerOrSpooler` | PASS |

RED was captured before implementation: 3 tests failed because `PrintServer` was rendered, `FakePrintSpoolerService` was registered, and production validation required four `PrintAgent` settings.

GREEN command:

```text
dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj -c Release --filter FullyQualifiedName~WebPrintingOnlyTests
```

Result: 3 passed, 0 failed, 0 skipped.

`BoxPrintJobs` and `PrinterConfigurations` are active again. The retained guarantees are that the HTML label still exposes `window.print()` and production startup does not depend on a local Windows spooler.
