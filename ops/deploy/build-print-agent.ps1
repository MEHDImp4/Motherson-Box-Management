param(
    [string] $Configuration = "Release",
    [string] $OutputDirectory = "MothersonBoxManagement/App_Data/Downloads"
)

$ErrorActionPreference = "Stop"
$project = "MothersonBoxManagement.PrintAgent/MothersonBoxManagement.PrintAgent.csproj"
$publish = "artifacts/print-agent-publish"
New-Item -ItemType Directory -Force -Path $publish, $OutputDirectory | Out-Null
dotnet publish $project -c $Configuration -r win-x64 --self-contained true -o $publish
$source = Join-Path $publish "MothersonBoxManagement.PrintAgent.exe"
$target = Join-Path $OutputDirectory "MothersonPrintAgentSetup.exe"
Copy-Item -LiteralPath $source -Destination $target -Force
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash
$version = (Get-Item -LiteralPath $source).VersionInfo.ProductVersion
@{ available = $true; version = $version; sha256 = $hash; signed = $false } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory "MothersonPrintAgentSetup.json") -Encoding utf8
Write-Host "Print agent: $target"
Write-Host "SHA-256: $hash"
