$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Invoke-DotNet @("restore", ".\Vexis.Engine2.slnx")
Invoke-DotNet @("build", ".\Vexis.Engine2.slnx", "-c", "Release", "--no-restore")
Invoke-DotNet @("test", ".\Vexis.Engine2.slnx", "-c", "Release", "--no-build")

Write-Host "Vexis Engine 2 foundation built and tested successfully." -ForegroundColor Green
