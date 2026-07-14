# Package-to-box scanner focus regression

## Source and user journey

These journeys were derived from the reported scanner behavior:

- As an operator, after scanning a package and seeing the prompt to scan a box, I want the scanner listener to remain active so the box barcode is captured immediately.
- As an operator, I want every package to start a new package-to-box cycle so it can be assigned to a different box from the preceding package.

## Evidence

| Guarantee | Test or command | Type | Result |
|---|---|---|---|
| The hidden scanner input is focused immediately and remains focused while the package overlay waits for a box | `node --test MothersonBoxManagement.Tests/Client/scanner.test.js` | Client unit regression | PASS |
| A second package is not associated with the previous box and prompts for a new box scan | `node --test MothersonBoxManagement.Tests/Client/scanner.test.js` | Client workflow regression | PASS |
| The application and existing server-side behavior still build and pass | `dotnet test Motherson_Box_Management.sln --no-restore` | Regression suite | PASS: 155, skipped: 2 |

## RED / GREEN

- RED 1: the client test observed that the visible scanner overlay retained focus after the package scan instead of the hidden scanner input.
- GREEN 1: `focusScanInput()` now treats the awaiting-box scanner overlay as an active capture state and `showPieceDetected()` restores scanner focus immediately.
- RED 2: after one successful package/box pair, scanning a second package issued a second association request against the remembered box.
- GREEN 2: the client no longer persists a current box; after association it returns to idle and the next package opens a fresh box prompt.

## Coverage and known gaps

The focused browser-state regression and the full .NET suite passed. The two existing SQL Server integration tests remain skipped because they require the external SQL integration environment. No repository-wide JavaScript coverage threshold is configured.
