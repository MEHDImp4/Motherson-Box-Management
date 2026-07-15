# Local Windows print agent — TDD evidence

## User journeys

- As an operator, I can queue a 100 × 100 mm label to the configured workstation without blocking box creation or scanning.
- As an administrator, I can pair one agent with a temporary code, choose a reported printer and select Windows or ZPL mode.
- As an operator, I retain browser printing when the agent is offline.
- As an agent, I can only claim jobs assigned to my workstation and report sanitized outcomes.

## RED/GREEN report

| Guarantee | RED evidence | GREEN evidence |
| --- | --- | --- |
| Pairing is single-use, expires after 10 minutes, and stores only a token hash | `CS0234`: `MothersonBoxManagement.Printing` missing | `PrintAgentWorkflowTests`: 4/4 passed |
| Agent API pairs, heartbeats, authenticates and claims station-scoped work | service missing and `/api/print-agent/*` returned 404 | `PrintAgentApiTests`: 2/2 passed |
| Only administrators configure/pair; downloads require authentication; UI and manual queue remain role-correct | admin routes returned 404 | `PrintAgentAdminTests`: 5/5 passed |
| Template creation queues a label for a configured workstation | `CS1729`: `BoxTemplateService` had no print service constructor | targeted test: 1/1 passed |
| ZPL is fixed at 800 × 800, escapes `^`, `~`, `\\`, and backoff is capped | core namespace/types missing | `PrintAgentRenderingTests`: 7/7 passed |
| The pairing action remains visible and can be triggered with Enter | `ConfigurationDialogLayout` missing (compile-time RED) | `PrintAgentRenderingTests`: 8/8 passed; agent build succeeded |
| A long build version cannot make the server reject pairing | `AgentVersionFormatter` missing (compile-time RED) | `PrintAgentRenderingTests`: 9/9 passed; agent build succeeded |
| An offline workstation exposes its pending labels in the Settings queue | `queue` property missing from `/PrintAgent/Status` | `PrintAgentAdminTests`: 6/6 passed |
| A missing selected printer leaves labels in `Pending` instead of claiming and failing them | Agent claimed the job despite the printer not being reported | `PrintAgentWorkflowTests`: 6/6 passed |
| Fleet supervision is a separate administrator-only page and includes agent, printer, queue, IP and last activity | `/PrintAgent/Workstations` missing and Settings mixed local/global configuration | `PrintAgentAdminTests`: 9/9 passed |

## Validation results

- `dotnet build Motherson_Box_Management.sln -c Release`: succeeded, 0 warnings and 0 errors.
- `dotnet ef migrations has-pending-model-changes`: no model changes pending.
- First full test run with coverage: 171 passed, 2 SQL tests skipped, 1 pre-existing concurrency test failed once because of shared test state; the same test passed immediately in isolation.
- Stable full rerun: 172 passed, 0 failed, 2 SQL tests skipped. After adding two final UI/manual-queue cases, their targeted class passed 5/5 and the final solution build remained clean; a further combined rerun was blocked by the local tool-usage quota rather than a test failure.
- New cross-platform agent core coverage: 81.25% lines. `PrintAgentService` class coverage: 92.85% lines in the generated Cobertura report.
- Self-contained `win-x64` EXE publish succeeded and the web publish contains the same artifact hash.
- Pairing dialog regression: the dialog now uses content-sized rows, has a 340 px minimum window height, and assigns the pairing action as its `AcceptButton`. The targeted test passed 8/8 and `dotnet build MothersonBoxManagement.PrintAgent/MothersonBoxManagement.PrintAgent.csproj --no-restore` completed with 0 warnings and 0 errors.
- Pairing version regression: the agent limits the product version it sends to the database/API limit of 40 characters. The targeted test passed 9/9 and the agent build completed with 0 warnings and 0 errors.
- Queue visibility: `/PrintAgent/Status` returns pending, claimed, failed and cancelled counts plus the last ten labels. `PrintAgentAdminTests` passed 6/6 and the web project built with 0 warnings and 0 errors.
- Unavailable printer queueing: the claim service returns no job while the selected printer is absent from the latest agent heartbeat, preserving labels as `Pending`. Combined print-agent workflow and admin tests passed 12/12; the web project built with 0 warnings and 0 errors.
- Agent branding: the generated Motherson Box logo is embedded as the Windows application icon and is extracted successfully from the published setup EXE for the notification-area icon. The web application retains its existing logo.
- Fleet supervision: administrators have a separate live workstation page with each agent's connection, assigned printer, queue counts, latest IP, activity and acting user. From this page they can remotely set the administrative station label, printer and Windows/ZPL mode. The table refreshes every five seconds; operators and supervisors do not receive the endpoint or page.

## Known gaps and acceptance gates

- WinForms, DPAPI, Windows spooler and physical printer behavior require the planned Windows 10/11 hardware acceptance test; they cannot be exercised by the Linux-compatible unit suite.
- Real SQL integration tests require `MOTHERSON_TEST_SQL_CONNECTION` and remain skipped when it is absent.
- The EXE is intentionally unsigned; CI verifies its SHA-256 and that the web publish embeds the exact same file. Windows may show a SmartScreen warning at first launch.
- TDD checkpoint commits were not created because the worktree already contained extensive unrelated and overlapping uncommitted changes; committing whole edited files would have captured pre-existing user work.
