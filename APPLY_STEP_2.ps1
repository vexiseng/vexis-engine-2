$ErrorActionPreference = "Stop"

$packageRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Get-Location).Path
$solutionPath = Join-Path $repoRoot "Vexis.Engine2.slnx"
$packagesPath = Join-Path $repoRoot "Directory.Packages.props"
$renderingProjectPath = Join-Path $repoRoot "src\Vexis.Rendering\Vexis.Rendering.csproj"

if (-not (Test-Path $solutionPath)) {
    throw "Run this script from the root of the vexis-engine-2 repository."
}

if (-not (Test-Path $renderingProjectPath)) {
    throw "Step 1 is missing. Expected: $renderingProjectPath"
}

$payload = Join-Path $packageRoot "payload"
Copy-Item (Join-Path $payload "*") $repoRoot -Recurse -Force

function Ensure-PackageVersion {
    param(
        [xml]$Xml,
        [string]$Name,
        [string]$Version
    )

    $existing = $Xml.Project.ItemGroup.PackageVersion |
        Where-Object { $_.Include -eq $Name } |
        Select-Object -First 1

    if ($null -ne $existing) {
        $existing.Version = $Version
        return
    }

    $itemGroup = $Xml.Project.ItemGroup |
        Where-Object { $null -ne $_.PackageVersion } |
        Select-Object -First 1

    if ($null -eq $itemGroup) {
        $itemGroup = $Xml.CreateElement("ItemGroup")
        [void]$Xml.Project.AppendChild($itemGroup)
    }

    $node = $Xml.CreateElement("PackageVersion")
    $node.SetAttribute("Include", $Name)
    $node.SetAttribute("Version", $Version)
    [void]$itemGroup.AppendChild($node)
}

function Ensure-PackageReference {
    param(
        [xml]$Xml,
        [string]$Name
    )

    $existing = $Xml.Project.ItemGroup.PackageReference |
        Where-Object { $_.Include -eq $Name } |
        Select-Object -First 1

    if ($null -ne $existing) {
        return
    }

    $itemGroup = $Xml.Project.ItemGroup |
        Where-Object { $null -ne $_.PackageReference } |
        Select-Object -First 1

    if ($null -eq $itemGroup) {
        $itemGroup = $Xml.CreateElement("ItemGroup")
        [void]$Xml.Project.AppendChild($itemGroup)
    }

    $node = $Xml.CreateElement("PackageReference")
    $node.SetAttribute("Include", $Name)
    [void]$itemGroup.AppendChild($node)
}

function Save-Xml {
    param(
        [xml]$Xml,
        [string]$Path
    )

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.OmitXmlDeclaration = $true
    $settings.NewLineChars = "`n"
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)

    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $Xml.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

[xml]$packages = Get-Content $packagesPath -Raw
Ensure-PackageVersion $packages "Vortice.Direct3D11" "3.8.3"
Ensure-PackageVersion $packages "Vortice.DXGI" "3.8.3"
Save-Xml $packages $packagesPath

[xml]$renderingProject = Get-Content $renderingProjectPath -Raw
Ensure-PackageReference $renderingProject "Vortice.Direct3D11"
Ensure-PackageReference $renderingProject "Vortice.DXGI"
Save-Xml $renderingProject $renderingProjectPath

Write-Host "Step 2 files applied." -ForegroundColor Green
Write-Host "Restoring packages..." -ForegroundColor Cyan
& dotnet restore .\Vexis.Engine2.slnx
if ($LASTEXITCODE -ne 0) { throw "Restore failed with exit code $LASTEXITCODE" }

Write-Host "Building solution..." -ForegroundColor Cyan
& dotnet build .\Vexis.Engine2.slnx -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

Write-Host "Running tests..." -ForegroundColor Cyan
& dotnet test .\Vexis.Engine2.slnx -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests failed with exit code $LASTEXITCODE" }

Write-Host ""
Write-Host "Step 2 completed successfully." -ForegroundColor Green
Write-Host "This step creates a real Direct3D 11 device and HWND swap chain," -ForegroundColor Green
Write-Host "but does not embed it into the Avalonia viewport yet." -ForegroundColor Yellow
