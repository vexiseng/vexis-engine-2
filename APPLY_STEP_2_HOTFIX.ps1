$ErrorActionPreference = "Stop"

$repoRoot = (Get-Location).Path
$file = Join-Path $repoRoot "src\Vexis.Rendering\Direct3D11GraphicsDevice.cs"

if (-not (Test-Path $file)) {
    throw "Run this from the vexis-engine-2 repository root. Missing: $file"
}

$content = Get-Content $file -Raw
$old = "ViewportSize.CreateValidated(width, height)"
$new = "new ViewportSize(width, height).ClampToValid()"

$count = ([regex]::Matches($content, [regex]::Escape($old))).Count
if ($count -eq 0) {
    Write-Host "ViewportSize hotfix appears to already be applied." -ForegroundColor Yellow
}
else {
    $content = $content.Replace($old, $new)
    Set-Content -Path $file -Value $content -Encoding utf8 -NoNewline
    Write-Host "Replaced $count invalid ViewportSize.CreateValidated call(s)." -ForegroundColor Green
}

Write-Host "Building solution..." -ForegroundColor Cyan
& dotnet build .\Vexis.Engine2.slnx -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Running tests..." -ForegroundColor Cyan
& dotnet test .\Vexis.Engine2.slnx -c Release --no-build
if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE"
}

Write-Host "Step 2 ViewportSize hotfix completed successfully." -ForegroundColor Green
