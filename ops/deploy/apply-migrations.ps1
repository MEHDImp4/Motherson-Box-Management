param(
    [Parameter(Mandatory = $true)] [string] $ProjectPath,
    [Parameter(Mandatory = $true)] [string] $ConnectionString
)

$ErrorActionPreference = 'Stop'
$env:ConnectionStrings__DefaultConnection = $ConnectionString
dotnet ef migrations script --idempotent --project $ProjectPath --startup-project $ProjectPath --output migration.sql
Write-Host 'Review migration.sql and confirm that a verified backup exists before applying it.'
Write-Host 'Apply through the approved DBA process, then run the SQL integration and smoke tests.'
Remove-Item Env:ConnectionStrings__DefaultConnection
