<#
.SYNOPSIS
    Verifies the generated SPDX 2.2 and 3.0 manifests and their relationship to the shipped package.

.DESCRIPTION
    Requires parseable, structurally valid documents, verifies each manifest against its SHA-256 sidecar,
    and proves that the SPDX 2.2 file entry describes the exact package bytes being released.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputRoot,

    [Parameter(Mandatory)]
    [string]$ExpectedPackagePath
)

$ErrorActionPreference = 'Stop'

$manifests = @(
    [pscustomobject]@{
        Name = 'SPDX 2.2'
        Path = Join-Path $OutputRoot 'spdx-2.2/_manifest/spdx_2.2/manifest.spdx.json'
    },
    [pscustomobject]@{
        Name = 'SPDX 3.0'
        Path = Join-Path $OutputRoot 'spdx-3.0/_manifest/spdx_3.0/manifest.spdx.json'
    }
)

$problems = @()
$documents = @{}
foreach ($manifest in $manifests) {
    if (-not (Test-Path -LiteralPath $manifest.Path -PathType Leaf)) {
        $problems += "$($manifest.Name) document missing: $($manifest.Path)"
        continue
    }

    if ((Get-Item -LiteralPath $manifest.Path).Length -eq 0) {
        $problems += "$($manifest.Name) document is empty: $($manifest.Path)"
        continue
    }

    try {
        $documents[$manifest.Name] = Get-Content -LiteralPath $manifest.Path -Raw | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        $problems += "$($manifest.Name) document is not valid JSON: $($_.Exception.Message)"
    }

    $checksumPath = "$($manifest.Path).sha256"
    if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) {
        $problems += "$($manifest.Name) checksum missing: $checksumPath"
        continue
    }

    $recordedChecksum = (Get-Content -LiteralPath $checksumPath -Raw).Trim().ToLowerInvariant()
    if ($recordedChecksum -notmatch '^[0-9a-f]{64}$') {
        $problems += "$($manifest.Name) checksum sidecar does not contain one SHA-256 digest: $checksumPath"
        continue
    }

    $actualChecksum = (Get-FileHash -LiteralPath $manifest.Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($recordedChecksum -ne $actualChecksum) {
        $problems += "$($manifest.Name) checksum is $recordedChecksum, but the document hash is $actualChecksum."
    }
}

if ($documents.ContainsKey('SPDX 2.2')) {
    $spdx22 = $documents['SPDX 2.2']
    if ($spdx22.spdxVersion -ne 'SPDX-2.2') {
        $problems += "SPDX 2.2 document declares version '$($spdx22.spdxVersion)'."
    }
    if (@($spdx22.packages).Count -eq 0) {
        $problems += 'SPDX 2.2 document contains no packages.'
    }
    if (@($spdx22.files).Count -eq 0) {
        $problems += 'SPDX 2.2 document contains no files.'
    }
}

if ($documents.ContainsKey('SPDX 3.0')) {
    $spdx30 = $documents['SPDX 3.0']
    if (@($spdx30.'@context').Count -eq 0) {
        $problems += 'SPDX 3.0 document contains no @context.'
    }
    if (@($spdx30.'@graph').Count -eq 0) {
        $problems += 'SPDX 3.0 document contains no @graph entries.'
    }
}

if (-not (Test-Path -LiteralPath $ExpectedPackagePath -PathType Leaf)) {
    $problems += "Expected NuGet package missing: $ExpectedPackagePath"
}
elseif ($documents.ContainsKey('SPDX 2.2')) {
    $expectedFileName = [System.IO.Path]::GetFileName($ExpectedPackagePath)
    $expectedPackageHash = (Get-FileHash -LiteralPath $ExpectedPackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $packageFiles = @(
        $documents['SPDX 2.2'].files | Where-Object {
            $_.fileName -and [System.IO.Path]::GetFileName([string]$_.fileName) -eq $expectedFileName
        }
    )

    if ($packageFiles.Count -ne 1) {
        $problems += "SPDX 2.2 document contains $($packageFiles.Count) file entries for $expectedFileName; expected exactly one."
    }
    else {
        $recordedPackageHashes = @(
            $packageFiles[0].checksums |
                Where-Object { $_.algorithm -eq 'SHA256' } |
                ForEach-Object { ([string]$_.checksumValue).ToLowerInvariant() }
        )

        if ($expectedPackageHash -notin $recordedPackageHashes) {
            $problems += "SPDX 2.2 records SHA-256 '$($recordedPackageHashes -join ', ')' for $expectedFileName, but the package hash is $expectedPackageHash."
        }
    }
}

if ($problems.Count -gt 0) {
    throw "SBOM validation failed:`n- $($problems -join "`n- ")"
}

Write-Host "SBOM covers $(@($documents['SPDX 2.2'].packages).Count) packages and $(@($documents['SPDX 2.2'].files).Count) files, including the verified NuGet package."
