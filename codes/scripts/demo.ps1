param(
    [ValidateSet('start', 'stop', 'status')][string]$Action = 'status',
    [string]$ConfigPath = 'demo.local.json',
    [string]$SqlServer = '.\SQLEXPRESS',
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,100}$')][string]$DatabaseName = 'HospitalDemo',
    [ValidateRange(1024, 65535)][int]$ApiPort = 5014,
    [ValidateRange(1024, 65535)][int]$WebPort = 5174,
    [string]$StateDirectory = 'tmp/demo',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'demo-config.ps1')
$configuration = $null
if ($Action -eq 'start') {
    $configuration = Get-DemoConfiguration -Workspace $workspace -ConfigPath $ConfigPath
    foreach ($key in @('SqlServer', 'DatabaseName', 'ApiPort', 'WebPort')) {
        if (!$PSBoundParameters.ContainsKey($key)) { Set-Variable -Name $key -Value $configuration[$key] }
    }
}
$stateRoot = [IO.Path]::GetFullPath((Join-Path $workspace $StateDirectory))
if (!$stateRoot.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'StateDirectory must stay inside the workspace.' }
$stateFile = Join-Path $stateRoot 'session.json'
Get-Command dotnet -ErrorAction Stop | Out-Null

function Read-State {
    if (Test-Path -LiteralPath $stateFile) {
        $saved = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
        if ($saved.workspace -ne $workspace) { throw 'Session belongs to another workspace.' }
        return $saved
    }
    return $null
}
function Owned-Process($Entry) {
    $running = Get-Process -Id $Entry.id -ErrorAction SilentlyContinue
    if ($null -eq $running) { return $null }
    if ($running.ProcessName -ne 'dotnet' -or $running.StartTime.ToUniversalTime().ToString('o') -ne $Entry.startedUtc) { return $null }
    $command = (Get-CimInstance Win32_Process -Filter "ProcessId=$($Entry.id)").CommandLine
    if ($command -notlike "*$($Entry.assembly)*") { throw 'Process identity cannot be verified; nothing was stopped.' }
    return $running
}
function Wait-Ready([string]$Url, [System.Diagnostics.Process]$Process) {
    $probe = [UriBuilder]$Url
    if ($probe.Host -eq 'localhost') { $probe.Host = '127.0.0.1' }
    $Url = $probe.Uri.AbsoluteUri
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.Elapsed.TotalSeconds -lt 90) {
        if ($Process.HasExited) { throw "Service exited. See logs in $stateRoot" }
        try { if ((Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200) { return } } catch { }
        Start-Sleep -Milliseconds 500
    }
    throw "Readiness timed out: $Url. See logs in $stateRoot"
}

$previous = Read-State
if ($Action -ne 'start') {
    if ($null -eq $previous) { Write-Output 'No recorded demo session.'; exit 0 }
    foreach ($entry in $previous.processes) {
        $running = Owned-Process $entry
        if ($Action -eq 'stop' -and $null -ne $running) {
            Stop-Process -Id $running.Id
            $running.WaitForExit(10000) | Out-Null
            Write-Output "Stopped $($entry.name) (PID $($entry.id))."
        }
        elseif ($Action -eq 'status') { Write-Output "$($entry.name): $(if ($null -ne $running) { 'running' } else { 'stopped' }) - $($entry.url)" }
    }
    if ($Action -eq 'stop') { Write-Output 'Database retained. Start again to continue with the same data.' }
    exit 0
}

if ($ApiPort -eq $WebPort) { throw 'API and Web ports must differ.' }
if ($null -ne $previous) {
    foreach ($entry in $previous.processes) { if ($null -ne (Owned-Process $entry)) { throw 'A recorded demo process is still running. Use status or stop first.' } }
}
foreach ($port in @($ApiPort, $WebPort)) {
    if (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) { throw "Port $port is in use. Select another port; existing processes are not stopped." }
}
if (!$SkipBuild) {
    Push-Location $workspace
    try {
        & dotnet build HospitalAppointmentSystem.sln --no-restore --no-incremental --disable-build-servers -m:1
        if ($LASTEXITCODE -ne 0) { throw 'Build failed. Run dotnet restore first when setting up a new machine.' }
    } finally { Pop-Location }
}
foreach ($project in @('Hospital.Api', 'Hospital.Web')) {
    if (!(Test-Path -LiteralPath (Join-Path $workspace "src/$project/bin/Debug/net9.0/$project.dll"))) { throw "Missing build output: $project" }
}
New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
$logRoot = Join-Path $stateRoot ([guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $logRoot | Out-Null
$keys = @('ASPNETCORE_ENVIRONMENT', 'ConnectionStrings__DefaultConnection', 'Database__Initialize', 'Database__SeedDemo', 'Api__BaseUrl')
$savedEnvironment = @{}
foreach ($key in $keys) { $savedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key) }
$started = @()
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection) -and ![string]::IsNullOrWhiteSpace($configuration.ConnectionString)) {
        $env:ConnectionStrings__DefaultConnection = $configuration.ConnectionString
    }
    if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
        $connection = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        $connection['Data Source'] = $SqlServer; $connection['Initial Catalog'] = $DatabaseName
        $connection['Integrated Security'] = $true; $connection['TrustServerCertificate'] = $true
        $env:ConnectionStrings__DefaultConnection = $connection.ConnectionString
    }
    $env:Database__Initialize = 'true'; $env:Database__SeedDemo = 'true'
    $env:Api__BaseUrl = "http://localhost:$ApiPort/"
    $entries = @()
    foreach ($service in @(@{ name = 'Hospital.Api'; port = $ApiPort; ready = '/health/ready' }, @{ name = 'Hospital.Web'; port = $WebPort; ready = '/Account/Login' })) {
        $assembly = "$($service.name).dll"; $url = "http://localhost:$($service.port)"
        $process = Start-Process dotnet -ArgumentList @("bin/Debug/net9.0/$assembly", '--urls', $url) -WorkingDirectory (Join-Path $workspace "src/$($service.name)") -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logRoot "$($service.name).log") -RedirectStandardError (Join-Path $logRoot "$($service.name)-error.log")
        $null = $process.Handle; $started += $process
        $entries += @{ name = $service.name; id = $process.Id; startedUtc = $process.StartTime.ToUniversalTime().ToString('o'); assembly = $assembly; url = $url }
        @{ workspace = $workspace; processes = $entries; logs = $logRoot } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $stateFile -Encoding UTF8
        Wait-Ready ($url + $service.ready) $process
    }
    Write-Output "API: http://localhost:$ApiPort/swagger"
    Write-Output "Web: http://localhost:$WebPort"
    Write-Output "Desktop: set HOSPITAL_API_URL=http://localhost:$ApiPort/ then run Hospital.Desktop."
    Write-Output "Logs: $logRoot"
    Write-Output 'Use demo.ps1 -Action stop to stop this session. The database is retained.'
}
catch {
    foreach ($process in $started) { if (!$process.HasExited) { Stop-Process -Id $process.Id; $process.WaitForExit(10000) | Out-Null } }
    throw
}
finally {
    foreach ($process in $started) { $process.Dispose() }
    foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $savedEnvironment[$key]) }
}
