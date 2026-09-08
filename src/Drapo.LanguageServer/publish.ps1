<#
.SYNOPSIS
  Publishes Drapo.LanguageServer as a self-contained build for each supported platform so the
  VS Code extension can bundle it (no .NET runtime needed on the user's machine).
.PARAMETER Rid
  Publish a single runtime identifier instead of all four.
.PARAMETER Configuration
  Build configuration (default Release).
#>
param(
    [ValidateSet('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64')][string]$Rid,
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'Drapo.LanguageServer.csproj'
$rids = if ($Rid) { @($Rid) } else { @('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64') }
foreach ($r in $rids) {
    $out = Join-Path $PSScriptRoot "bin\publish\$r"
    Write-Host "Publishing $r -> $out"
    # Trimming/AOT are off on purpose: the engine catalog reads an embedded resource through
    # reflection and the LSP framework relies on reflection-based JSON (see research.md R8).
    dotnet publish $project -c $Configuration -r $r --self-contained true `
        -p:PublishSingleFile=false -p:PublishTrimmed=false -p:PublishReadyToRun=false `
        -o $out
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $r" }
}
