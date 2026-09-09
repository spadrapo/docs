<#
.SYNOPSIS
  Builds the Drapo Visual Studio extension (VSIX): publishes the language server self-contained
  for win-x64, copies it (with its documentation content) under server/, and runs MSBuild.
  Used locally and by the CI 'vs-extension' job.
.PARAMETER Version
  VSIX version (2-4 numeric parts; a prerelease suffix such as -beta.1 is dropped). Defaults to
  the VS Code extension's package.json version with a trailing .0, so both editors ship one
  version number per release.
.PARAMETER Configuration
  Build configuration (default Release).
.PARAMETER SkipPublish
  Reuse an existing server/ folder instead of publishing the server again.
.OUTPUTS
  bin/<Configuration>/Drapo.VisualStudio.vsix, renamed to Drapo.VisualStudio-<version>.vsix in the project folder.
#>
param(
    [string]$Version,
    [string]$Configuration = 'Release',
    [switch]$SkipPublish
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$src = Split-Path -Parent $root

if (-not $Version) {
    $package = Get-Content (Join-Path $src 'vscode-drapo\package.json') -Raw | ConvertFrom-Json
    $Version = "$($package.version).0"
}
# VSIX versions are numeric only (2-4 parts): drop a leading 'v' and any prerelease/build suffix.
$Version = ($Version -replace '^v', '') -replace '[-+].*$', ''
if ($Version -notmatch '^\d+(\.\d+){1,3}$') { throw "'$Version' is not a valid VSIX version (2-4 numeric parts, e.g. 0.2.0.0)." }

$serverDir = Join-Path $root 'server'
if (-not $SkipPublish) {
    $publish = Join-Path $src 'Drapo.LanguageServer\bin\publish\win-x64'
    & (Join-Path $src 'Drapo.LanguageServer\publish.ps1') -Rid win-x64 -Configuration $Configuration
    if (Test-Path $serverDir) { Remove-Item -Recurse -Force $serverDir }
    New-Item -ItemType Directory -Force $serverDir | Out-Null
    Copy-Item -Recurse -Force (Join-Path $publish '*') $serverDir
    Write-Host "Copied $publish -> $serverDir"
}
if (-not (Test-Path (Join-Path $serverDir 'Drapo.LanguageServer.exe'))) { throw "server/Drapo.LanguageServer.exe not found; run without -SkipPublish." }

# MSBuild from the newest Visual Studio (the VSSDK targets need the full MSBuild, not `dotnet build`'s).
$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
if (-not $msbuild) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    }
}
if (-not $msbuild) { throw 'MSBuild.exe not found. Install Visual Studio with the "Visual Studio extension development" workload, or run from a Developer PowerShell.' }

$project = Join-Path $root 'Drapo.VisualStudio.csproj'
Write-Host "Building $project version $Version with $msbuild"
& $msbuild $project -restore -t:Rebuild -p:Configuration=$Configuration -p:DeployExtension=false "-p:VsixVersion=$Version" -v:minimal -nologo
if ($LASTEXITCODE -ne 0) { throw "msbuild failed ($LASTEXITCODE)" }

$built = Join-Path $root "bin\$Configuration\net472\Drapo.VisualStudio.vsix"
if (-not (Test-Path $built)) { throw "VSIX not produced at $built" }
$target = Join-Path $root "Drapo.VisualStudio-$Version.vsix"
Copy-Item -Force $built $target
Write-Host "Created $target ($([math]::Round((Get-Item $target).Length / 1MB, 1)) MB)"
