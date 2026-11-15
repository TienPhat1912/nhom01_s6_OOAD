$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')).TrimEnd([IO.Path]::DirectorySeparatorChar)
$parent = Join-Path $root '.local'
$scratch = [IO.Path]::GetFullPath((Join-Path $parent ('setup check ' + [guid]::NewGuid().ToString('N'))))
$allowed = [IO.Path]::GetFullPath($parent).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (!$scratch.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Test path escaped codes/.local.' }
function Assert-Setup([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
try {
    $setupDirectory = Join-Path $scratch 'scripts'
    New-Item -ItemType Directory -Force -Path $setupDirectory | Out-Null
    foreach ($name in @('setup.ps1', 'demo-config.ps1')) { Copy-Item -LiteralPath (Join-Path $root "scripts/$name") -Destination $setupDirectory }
    Copy-Item -LiteralPath (Join-Path $root 'demo.example.json') -Destination $scratch
    $originalLocation = (Get-Location).Path
    & (Join-Path $setupDirectory 'setup.ps1') -ConfigOnly
    Assert-Setup ((Get-Location).Path -eq $originalLocation) 'Setup must restore the caller location.'
    . (Join-Path $setupDirectory 'demo-config.ps1')
    $settings = Get-DemoConfiguration -Workspace $scratch
    Assert-Setup ($settings.DatabaseName -eq 'HospitalDemo' -and $settings.ApiPort -eq 5014) 'Fresh configuration defaults are incorrect.'
    $settings.DatabaseName = 'TeamDemo'
    $settings.ApiPort = 5514
    $settings.WebPort = 5574
    $settings.ConnectionString = 'Server=example.invalid;Database=TeamDemo;User Id=demo;Password=ExampleOnly!123;'
    $path = Join-Path $scratch 'demo.local.json'
    [IO.File]::WriteAllText($path, ($settings | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))
    $before = (Get-FileHash -LiteralPath $path).Hash
    & (Join-Path $setupDirectory 'setup.ps1') -ConfigOnly
    Assert-Setup ((Get-FileHash -LiteralPath $path).Hash -eq $before) 'Setup must not overwrite local configuration.'
    $again = Get-DemoConfiguration -Workspace $scratch
    Assert-Setup ($again.ApiPort -eq 5514 -and $again.WebPort -eq 5574 -and $again.ConnectionString -eq $settings.ConnectionString) 'Custom settings were not read.'
    foreach ($invalid in @('{"ApiPort":5174}', '{"WebPort":70000}', '{"DatabaseName":"bad-name"}', '{"ApiPort":true}', 'null', '[]', '{broken json')) {
        [IO.File]::WriteAllText($path, $invalid)
        $rejected = $false
        try { $null = Get-DemoConfiguration -Workspace $scratch } catch { $rejected = $true }
        Assert-Setup $rejected 'Invalid configuration was accepted.'
    }
    $rejected = $false
    try { $null = Get-DemoConfiguration -Workspace $scratch -ConfigPath '../outside.json' } catch { $rejected = $true }
    Assert-Setup $rejected 'Configuration path outside codes was accepted.'
    Write-Output 'PASS: fresh setup, spaces in path, preserved settings/location, custom ports/connection and invalid configuration.'
} finally {
    if ($scratch.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $scratch)) {
        Remove-Item -LiteralPath $scratch -Recurse -Force
    }
}
