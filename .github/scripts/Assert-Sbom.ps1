<#
.SYNOPSIS
    Asserts the generated SPDX 2.2 and 3.0 manifests exist, are non-empty, and describe the shipped package.

.DESCRIPTION
    sbom-tool exits 0 for a document that lists nothing useful, so the release gate is this check rather
    than the tool's exit code.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputRoot,

    # The nupkg that must appear in the SPDX 2.2 files section; omit to only check the documents exist.
    [string]$ExpectedFileName
)

$ErrorActionPreference = 'Stop'

$manifests = @(
    (Join-Path $OutputRoot 'spdx-2.2/_manifest/spdx_2.2/manifest.spdx.json'),
    (Join-Path $OutputRoot 'spdx-3.0/_manifest/spdx_3.0/manifest.spdx.json')
)

$problems = @()
foreach ($manifest in $manifests) {
    foreach ($file in @($manifest, "$manifest.sha256")) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            $problems += "SBOM file missing: $file"
        }
        elseif ((Get-Item -LiteralPath $file).Length -eq 0) {
            $problems += "SBOM file is empty: $file"
        }
    }
}

if ($problems.Count -gt 0) {
    throw "SBOM validation failed:`n- $($problems -join "`n- ")"
}

$spdx = Get-Content -LiteralPath $manifests[0] -Raw | ConvertFrom-Json

if ($ExpectedFileName -and -not ($spdx.files | Where-Object { $_.fileName -like "*$ExpectedFileName" })) {
    throw "The SPDX 2.2 document does not reference $ExpectedFileName."
}

Write-Host "SBOM covers $($spdx.packages.Count) packages and $($spdx.files.Count) files."
