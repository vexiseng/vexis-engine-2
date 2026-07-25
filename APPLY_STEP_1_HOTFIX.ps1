$ErrorActionPreference = "Stop"

$repoRoot = (Get-Location).Path
$testDir = Join-Path $repoRoot "tests\Vexis.Rendering.Tests"

if (-not (Test-Path $testDir)) {
    throw "Run this from the vexis-engine-2 repository root. Missing: $testDir"
}

$files = @(
    (Join-Path $testDir "GraphicsBackendSelectorTests.cs"),
    (Join-Path $testDir "RenderCameraTests.cs")
)

foreach ($file in $files) {
    if (-not (Test-Path $file)) {
        throw "Missing expected test file: $file"
    }

    $content = Get-Content $file -Raw
    if ($content -notmatch '(?m)^using Xunit;\s*$') {
        Set-Content -Path $file -Value ("using Xunit;`r`n" + $content) -NoNewline
        Write-Host "Added using Xunit; to $file"
    }
    else {
        Write-Host "Already fixed: $file"
    }
}

Write-Host "Building solution..."
dotnet build .\Vexis.Engine2.slnx -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

Write-Host "Running rendering tests..."
dotnet test .\tests\Vexis.Rendering.Tests\Vexis.Rendering.Tests.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests failed with exit code $LASTEXITCODE" }

Write-Host "Step 1 hotfix applied successfully."
