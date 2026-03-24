[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$PackagePath,

    [string]$Source = "https://nuget.digitalfactory.vn/v3/index.json",

    [string]$ApiKey = $env:NUGET_API_KEY,

    [switch]$IncludeSymbols,

    [switch]$SkipDuplicate,

    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-Packages {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [switch]$IncludeSymbols
    )

    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        return @(Get-Item -LiteralPath $Path)
    }

    if (Test-Path -LiteralPath $Path -PathType Container) {
        if ($IncludeSymbols) {
            return @(Get-ChildItem -LiteralPath $Path -Filter "*.nupkg" -File | Sort-Object Name)
        }

        return @(Get-ChildItem -LiteralPath $Path -Filter "*.nupkg" -File | Where-Object { $_.Name -notlike "*.snupkg" } | Sort-Object Name)
    }

    # Supports wildcards such as .\nuget-output\*.nupkg
    $resolved = @(Get-Item -Path $Path -ErrorAction SilentlyContinue)
    if (-not $IncludeSymbols) {
        $resolved = @($resolved | Where-Object { $_.Name -notlike "*.snupkg" })
    }

    return $resolved
}

$packages = @(Get-Packages -Path $PackagePath -IncludeSymbols:$IncludeSymbols)
if (-not $packages -or $packages.Length -eq 0) {
    throw "No packages found for '$PackagePath'."
}

# if (-not $ApiKey) { local nuget doesnt have api key
#     throw "API key is missing. Pass -ApiKey or set NUGET_API_KEY environment variable."
# }

foreach ($package in $packages) {
    $arguments = @("nuget", "push", $package.FullName, "-s", $Source)

    if ($SkipDuplicate) {
        $arguments += "--skip-duplicate"
    }

    if ($DryRun) {
        Write-Host "[DRY RUN] dotnet $($arguments -join ' ')"
        continue
    }

    Write-Host "Pushing $($package.Name) to $Source"
    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet nuget push failed for '$($package.FullName)' with exit code $LASTEXITCODE."
    }
}

Write-Host "Done. Processed $($packages.Length) package(s)."

