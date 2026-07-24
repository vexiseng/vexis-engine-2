param(
    [string]$Request = "Place an oak tree at the selected location",
    [string]$Model = "qwen3:8b"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
$env:VEXIS_AI_MODEL = $Model

dotnet run --project .\src\Vexis.Editor.Host -- ai $Request
