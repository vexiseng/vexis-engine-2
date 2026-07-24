$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
dotnet run --project .\src\Vexis.Editor.Desktop\Vexis.Editor.Desktop.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Vexis Studio exited with code $LASTEXITCODE." }
