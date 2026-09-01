<#
.SYNOPSIS
    Copies the provenance and SBOM attestation bundles next to the SBOM documents and records their URLs.

.DESCRIPTION
    actions/attest writes each bundle to a temporary path that only the producing job can read, so the
    bundles are collected into the release artifact while they are still reachable.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ProvenanceBundlePath,

    [Parameter(Mandatory)]
    [string]$SbomBundlePath,

    [Parameter(Mandatory)]
    [string]$Destination,

    [string]$ProvenanceUrl,

    [string]$SbomUrl,

    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $Destination -Force | Out-Null
Copy-Item -LiteralPath $ProvenanceBundlePath -Destination (Join-Path $Destination 'provenance.sigstore.json') -Force
Copy-Item -LiteralPath $SbomBundlePath -Destination (Join-Path $Destination 'sbom.sigstore.json') -Force

if ($SummaryPath) {
    @(
        '### Attestations',
        "- Provenance: $ProvenanceUrl",
        "- SBOM: $SbomUrl"
    ) | Add-Content -LiteralPath $SummaryPath
}
