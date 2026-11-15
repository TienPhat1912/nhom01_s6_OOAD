param([switch]$ConfigOnly)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'demo-config.ps1')
Push-Location -LiteralPath $workspace
try {
    if (!(Test-Path -LiteralPath 'demo.local.json')) {
        Copy-Item -LiteralPath 'demo.example.json' -Destination 'demo.local.json'
    }
    $null = Get-DemoConfiguration -Workspace $workspace
    Write-Output 'Local configuration ready: demo.local.json (existing settings are preserved).'
    if ($ConfigOnly) { return }
    Get-Command dotnet -ErrorAction Stop | Out-Null
    $runtimes = & dotnet --list-runtimes
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect .NET runtimes.' }
    foreach ($runtime in @('Microsoft.NETCore.App', 'Microsoft.AspNetCore.App', 'Microsoft.WindowsDesktop.App')) {
        if (!($runtimes -match ('^' + [regex]::Escape($runtime) + ' 9\.'))) {
            throw "Install .NET 9 SDK for Windows (missing $runtime 9.x). See README.md."
        }
    }
    & dotnet restore HospitalAppointmentSystem.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed. Check the .NET 9 SDK and network connection.' }
    & dotnet tool restore --tool-manifest .config/dotnet-tools.json
    if ($LASTEXITCODE -ne 0) { throw 'EF tool restore failed.' }
    & dotnet build HospitalAppointmentSystem.sln --no-restore --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }
    Write-Output 'Ready. Configure SQL Server in demo.local.json, then run scripts/demo.ps1 -Action start -SkipBuild.'
} finally { Pop-Location }
