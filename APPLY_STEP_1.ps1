$ErrorActionPreference = 'Stop'

$packageRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Get-Location).Path
$solutionPath = Join-Path $repoRoot 'Vexis.Engine2.slnx'

if (-not (Test-Path $solutionPath)) {
    throw 'Run this script from the root of the vexis-engine-2 repository.'
}

$payload = Join-Path $packageRoot 'payload'
Copy-Item (Join-Path $payload '*') $repoRoot -Recurse -Force

[xml]$solution = Get-Content $solutionPath -Raw
$srcFolder = $solution.Solution.Folder | Where-Object { $_.Name -eq '/src/' }
$testsFolder = $solution.Solution.Folder | Where-Object { $_.Name -eq '/tests/' }

if ($null -eq $srcFolder -or $null -eq $testsFolder) {
    throw 'The solution layout is not the expected Vexis.Engine2.slnx structure.'
}

if (-not ($srcFolder.Project | Where-Object { $_.Path -eq 'src/Vexis.Rendering/Vexis.Rendering.csproj' })) {
    $project = $solution.CreateElement('Project')
    $project.SetAttribute('Path', 'src/Vexis.Rendering/Vexis.Rendering.csproj')
    [void]$srcFolder.AppendChild($project)
}

if (-not ($testsFolder.Project | Where-Object { $_.Path -eq 'tests/Vexis.Rendering.Tests/Vexis.Rendering.Tests.csproj' })) {
    $project = $solution.CreateElement('Project')
    $project.SetAttribute('Path', 'tests/Vexis.Rendering.Tests/Vexis.Rendering.Tests.csproj')
    [void]$testsFolder.AppendChild($project)
}

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.OmitXmlDeclaration = $true
$settings.NewLineChars = "`n"
$settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
$writer = [System.Xml.XmlWriter]::Create($solutionPath, $settings)
$solution.Save($writer)
$writer.Dispose()

Write-Host 'Step 1 files applied.' -ForegroundColor Green
Write-Host 'Building solution...' -ForegroundColor Cyan
& dotnet build .\Vexis.Engine2.slnx -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

Write-Host 'Running tests...' -ForegroundColor Cyan
& dotnet test .\Vexis.Engine2.slnx -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests failed with exit code $LASTEXITCODE" }

Write-Host 'Step 1 completed successfully.' -ForegroundColor Green
