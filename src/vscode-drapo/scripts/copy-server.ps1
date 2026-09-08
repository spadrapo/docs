<#
.SYNOPSIS
  Copies a self-contained published Drapo.LanguageServer into server/<rid>/ so that
  `vsce package --target <target>` bundles it. Clears every other server/* folder first,
  because each VSIX carries exactly one platform.
.PARAMETER Rid
  .NET runtime identifier: win-x64, linux-x64, osx-x64 or osx-arm64.
.PARAMETER Source
  Optional publish folder. Defaults to ../Drapo.LanguageServer/bin/publish/<rid>.
#>
param(
    [Parameter(Mandatory = $true)][ValidateSet('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64')][string]$Rid,
    [string]$Source
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $Source) { $Source = Join-Path $root "..\Drapo.LanguageServer\bin\publish\$Rid" }
if (-not (Test-Path $Source)) { throw "Publish output not found: $Source. Run ../Drapo.LanguageServer/publish.ps1 -Rid $Rid first." }

$serverRoot = Join-Path $root 'server'
if (Test-Path $serverRoot) { Remove-Item -Recurse -Force $serverRoot }
$target = Join-Path $serverRoot $Rid
New-Item -ItemType Directory -Force $target | Out-Null
Copy-Item -Recurse -Force (Join-Path $Source '*') $target
Write-Host "Copied $Source -> $target"
