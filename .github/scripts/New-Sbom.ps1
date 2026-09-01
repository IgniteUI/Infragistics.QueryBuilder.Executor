<#
.SYNOPSIS
    Generates SPDX 2.2 and SPDX 3.0 SBOMs for a packed NuGet package with the pinned sbom-tool.

.DESCRIPTION
    Shared by the release workflow and the on-demand SBOM dry run so both produce identical documents.
    'dotnet tool run' has no --tool-manifest option and discovers the manifest from the current
    directory, so the tool is invoked from the folder holding the nested manifest.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackageId,

    [Parameter(Mandatory)]
    [string]$PackageVersion,

    # -b: files under this path are hashed into the SBOM's files section.
    [Parameter(Mandatory)]
    [string]$BuildDropPath,

    # -bc: root the component detectors scan to build the dependency graph.
    [Parameter(Mandatory)]
    [string]$BuildComponentPath,

    [Parameter(Mandatory)]
    [string]$OutputRoot,

    [string]$Supplier = 'Infragistics Inc.',

    [string]$NamespaceBaseUri,

    # External license lookup is a network call per component; bound it or turn it off.
    [bool]$ResolveLicenses = $true,

    [int]$LicenseTimeoutSeconds = 180,

    [string]$ToolManifestDirectory = (Join-Path $PSScriptRoot '../../.config/sbom-tool')
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

if (-not $NamespaceBaseUri) {
    $NamespaceBaseUri = "http://spdx.org/spdxdocs/$PackageId"
}

$formats = @(
    @{ Version = 'SPDX:2.2'; Output = 'spdx-2.2' },
    @{ Version = 'SPDX:3.0'; Output = 'spdx-3.0' }
)

Push-Location -LiteralPath $ToolManifestDirectory
try {
    foreach ($format in $formats) {
        $manifestDirectory = Join-Path $OutputRoot $format.Output
        # sbom-tool requires the manifest directory to exist before generation.
        New-Item -ItemType Directory -Path $manifestDirectory -Force | Out-Null

        Write-Host "Generating $($format.Version) SBOM into $manifestDirectory..."
        dotnet tool run sbom-tool -- generate `
            -b $BuildDropPath `
            -bc $BuildComponentPath `
            -m $manifestDirectory `
            -pn $PackageId `
            -pv $PackageVersion `
            -ps $Supplier `
            -nsb $NamespaceBaseUri `
            -mi $format.Version `
            -li $ResolveLicenses.ToString().ToLowerInvariant() `
            -lto $LicenseTimeoutSeconds `
            -pm true `
            -V Information
    }
}
finally {
    Pop-Location
}
