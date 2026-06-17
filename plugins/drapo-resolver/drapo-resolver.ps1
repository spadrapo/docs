<#
.SYNOPSIS
    Project-aware Drapo resolver for any Drapo application: go-to-definition /
    find-references for app-specific Drapo symbols (data keys, components, sectors).

.DESCRIPTION
    Indexes a Drapo app's frontend (wwwroot/app + wwwroot/components) and answers
    structured (JSON) queries the shared docs MCP cannot: which d-dataKeys / components /
    sectors THIS app actually defined, and where they are referenced. Use it to confirm a
    binding target exists before claiming an edit is correct (catches plausible-but-wrong
    guesses).

.PARAMETER Command
    resolve-datakey | resolve-component | resolve-sector | find-references | scan-unresolved

.PARAMETER Name
    The symbol to resolve (data key, component tag/name, sector name). Omitted for scan-unresolved.

.PARAMETER Root
    wwwroot path. If omitted, auto-detected (a 'wwwroot' folder containing 'app/') under the
    current directory.

.EXAMPLE
    pwsh ./drapo-resolver.ps1 resolve-datakey myDataKey
    pwsh ./drapo-resolver.ps1 resolve-component d-mycomponent
    pwsh ./drapo-resolver.ps1 find-references myDataKey
    pwsh ./drapo-resolver.ps1 scan-unresolved
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet('resolve-datakey', 'resolve-component', 'resolve-sector', 'find-references', 'scan-unresolved')]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$Name,

    [string]$Root
)

$ErrorActionPreference = 'Stop'

function Find-DrapoRoot {
    # A Drapo wwwroot is a directory named 'wwwroot' that contains an 'app' folder
    # (and usually 'components'). Search under the current directory, skipping build output.
    $candidates = Get-ChildItem -Path (Get-Location) -Recurse -Directory -Filter 'wwwroot' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules)[\\/]' } |
        Where-Object { Test-Path (Join-Path $_.FullName 'app') }
    if ($candidates.Count -eq 1) { return $candidates[0].FullName }
    if ($candidates.Count -gt 1) {
        throw "Multiple Drapo wwwroot folders found; pass -Root explicitly:`n" + (($candidates.FullName) -join "`n")
    }
    throw "Could not locate a Drapo wwwroot (a 'wwwroot' folder containing 'app') under '$(Get-Location)'. Pass -Root explicitly."
}

if (-not $Root) { $Root = Find-DrapoRoot }
$Root = (Resolve-Path $Root).Path
$AppDir = Join-Path $Root 'app'
$ComponentsDir = Join-Path $Root 'components'

$TagRegex = [regex]'(?s)<[a-zA-Z][^>]*?>'

function Get-AppFiles { Get-ChildItem -Path $AppDir -Recurse -Filter *.html -File }
function Get-RelPath([string]$p) { ($p.Substring($Root.Length).TrimStart('\', '/')) -replace '\\', '/' }
function Get-LineNumber([string]$content, [int]$index) { (($content.Substring(0, $index)) -split "`n").Length }
function Get-AttrValue([string]$tag, [string]$attr) {
    $m = [regex]::Match($tag, '(?i)\b' + [regex]::Escape($attr) + '\s*=\s*"([^"]*)"')
    if ($m.Success) { return $m.Groups[1].Value } else { return $null }
}
function Get-ComponentFolders { (Get-ChildItem -Path $ComponentsDir -Directory).Name }
function Write-Result($obj) { $obj | ConvertTo-Json -Depth 8 }

function Invoke-ResolveDataKey([string]$key) {
    $decls = [System.Collections.Generic.List[object]]::new()
    foreach ($f in Get-AppFiles) {
        $c = Get-Content -Raw -LiteralPath $f.FullName
        foreach ($m in $TagRegex.Matches($c)) {
            $tag = $m.Value
            if ((Get-AttrValue $tag 'd-dataKey') -eq $key) {
                $decls.Add([pscustomobject]@{
                        file       = Get-RelPath $f.FullName
                        line       = Get-LineNumber $c $m.Index
                        dataType   = Get-AttrValue $tag 'd-dataType'
                        dataUrlGet = Get-AttrValue $tag 'd-dataUrlGet'
                        dataUrlSet = Get-AttrValue $tag 'd-dataUrlSet'
                        dataValue  = Get-AttrValue $tag 'd-dataValue'
                    })
            }
        }
    }
    Write-Result ([pscustomobject]@{ command = 'resolve-datakey'; query = $key; found = ($decls.Count -gt 0); declarations = @($decls) })
}

function Invoke-ResolveSector([string]$name) {
    $decls = [System.Collections.Generic.List[object]]::new()
    foreach ($f in Get-AppFiles) {
        $c = Get-Content -Raw -LiteralPath $f.FullName
        foreach ($m in $TagRegex.Matches($c)) {
            if ((Get-AttrValue $m.Value 'd-sector') -eq $name) {
                $decls.Add([pscustomobject]@{ file = Get-RelPath $f.FullName; line = Get-LineNumber $c $m.Index })
            }
        }
    }
    Write-Result ([pscustomobject]@{ command = 'resolve-sector'; query = $name; found = ($decls.Count -gt 0); declarations = @($decls) })
}

function Invoke-ResolveComponent([string]$tag) {
    $folder = ($tag -replace '^d-', '').ToLowerInvariant()
    $path = Join-Path $ComponentsDir $folder
    if (Test-Path -LiteralPath $path -PathType Container) {
        $files = (Get-ChildItem -LiteralPath $path -File | ForEach-Object { Get-RelPath $_.FullName })
        Write-Result ([pscustomobject]@{ command = 'resolve-component'; query = $tag; found = $true; component = $folder; folder = (Get-RelPath $path); files = @($files) })
    }
    else {
        Write-Result ([pscustomobject]@{ command = 'resolve-component'; query = $tag; found = $false; component = $folder })
    }
}

function Invoke-FindReferences([string]$key) {
    $esc = [regex]::Escape($key)
    $declRx = [regex]('(?i)d-dataKey\s*=\s*"' + $esc + '"')
    # The key used in a mustache, or as a value token inside any d-* attribute.
    $refRx = [regex]('(\{\{\s*' + $esc + '\b)|(?i)\bd-[a-z][\w-]*\s*=\s*"[^"]*\b' + $esc + '\b[^"]*"')
    $declarations = [System.Collections.Generic.List[object]]::new()
    $references = [System.Collections.Generic.List[object]]::new()
    foreach ($f in Get-AppFiles) {
        $rel = Get-RelPath $f.FullName
        $ln = 0
        foreach ($line in (Get-Content -LiteralPath $f.FullName)) {
            $ln++
            if ($declRx.IsMatch($line)) {
                $declarations.Add([pscustomobject]@{ file = $rel; line = $ln; text = $line.Trim() })
            }
            elseif ($refRx.IsMatch($line)) {
                $references.Add([pscustomobject]@{ file = $rel; line = $ln; text = $line.Trim() })
            }
        }
    }
    Write-Result ([pscustomobject]@{ command = 'find-references'; query = $key; declarationCount = $declarations.Count; referenceCount = $references.Count; declarations = @($declarations); references = @($references) })
}

function Invoke-ScanUnresolved {
    $componentSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($c in Get-ComponentFolders) { [void]$componentSet.Add($c) }
    $declaredSectors = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

    $compTagRx = [regex]'(?i)<d-([a-z0-9]+)'
    $sectorDeclRx = [regex]'(?i)d-sector\s*=\s*"([^"]+)"'
    $updateSectorRx = [regex]'(?i)UpdateSector\(\s*([A-Za-z0-9_]+)'

    $unresolvedComponents = [System.Collections.Generic.List[object]]::new()
    $sectorUsages = [System.Collections.Generic.List[object]]::new()

    # First pass: collect declared sectors.
    foreach ($f in Get-AppFiles) {
        $c = Get-Content -Raw -LiteralPath $f.FullName
        foreach ($m in $sectorDeclRx.Matches($c)) { [void]$declaredSectors.Add($m.Groups[1].Value) }
    }

    # Second pass: component tags with no folder + UpdateSector targets.
    foreach ($f in Get-AppFiles) {
        $rel = Get-RelPath $f.FullName
        $c = Get-Content -Raw -LiteralPath $f.FullName
        foreach ($m in $compTagRx.Matches($c)) {
            $comp = $m.Groups[1].Value
            if (-not $componentSet.Contains($comp)) {
                $unresolvedComponents.Add([pscustomobject]@{ tag = "d-$comp"; file = $rel; line = Get-LineNumber $c $m.Index })
            }
        }
        foreach ($m in $updateSectorRx.Matches($c)) {
            $sec = $m.Groups[1].Value
            if (-not $declaredSectors.Contains($sec)) {
                $sectorUsages.Add([pscustomobject]@{ sector = $sec; file = $rel; line = Get-LineNumber $c $m.Index })
            }
        }
    }

    Write-Result ([pscustomobject]@{
            command              = 'scan-unresolved'
            unresolvedComponents = @($unresolvedComponents)
            unresolvedSectorCandidates = @($sectorUsages)
            notes                = 'unresolvedComponents are high-confidence (no matching folder under wwwroot/components). unresolvedSectorCandidates are UpdateSector() targets with no static d-sector declaration - many are legitimate (declared in dynamically loaded/parent content), so treat as candidates to review, not errors.'
        })
}

switch ($Command) {
    'resolve-datakey' { if (-not $Name) { throw 'resolve-datakey requires a key name' }; Invoke-ResolveDataKey $Name }
    'resolve-sector' { if (-not $Name) { throw 'resolve-sector requires a sector name' }; Invoke-ResolveSector $Name }
    'resolve-component' { if (-not $Name) { throw 'resolve-component requires a component tag/name' }; Invoke-ResolveComponent $Name }
    'find-references' { if (-not $Name) { throw 'find-references requires a key name' }; Invoke-FindReferences $Name }
    'scan-unresolved' { Invoke-ScanUnresolved }
}
