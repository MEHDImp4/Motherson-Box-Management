# Production remediation TDD evidence

## Journeys

- Password changes must remain auditable without exposing hashes or secrets.
- Demo identities must use external, distinct credentials and must never be seeded in Production.
- Concurrent failed logins must not lose increments or bypass lockout.
- Active package prefixes must be deterministic and unique.
- Superseded on 13 July 2026: the Windows print agent was removed and printing is now browser-only.

## RED/GREEN evidence

| Guarantee | RED | GREEN |
|---|---|---|
| Audit excludes password hashes | `SecurityRemediationTests`: `PasswordHash` was found | Focused suite: 2/2 then 4/4 passed |
| Production rejects demo seed | No exception was thrown | Focused suite passed |
| External demo passwords required and distinct | Compile-time missing overload | Focused suite 4/4 passed |
| Concurrent lockout increments preserved | Missing cancellation-aware API, then reproduced case mismatch | Focused test 1/1 passed |
| Active prefix normalized and unique | Duplicate creation did not throw | Focused test 1/1 passed |
| Print job idempotence and allowlist | Missing agent contracts | Print-agent tests 2/2 passed |

## Broader verification

- `dotnet build Motherson_Box_Management.sln -c Release`: PASS, 0 warnings, 0 errors.
- Complete local suite after the first remediation wave: 162 passed, 2 conditional SQL tests skipped.
- SQL Server 2022 integration after recreating the test volume: 2 passed, 0 skipped.
- Docker Linux image build and Development healthcheck: PASS.

Physical printer, production certificates, production DNS/firewall, load, failover and an actual Windows service installation remain environment acceptance gates.
