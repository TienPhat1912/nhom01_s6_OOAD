param(
    [string]$SqlServer = '.\SQLEXPRESS',
    [ValidateRange(1024, 65535)][int]$ApiPort = 5034,
    [ValidateRange(1024, 65535)][int]$WebPort = 5175,
    [switch]$SkipBuild,
    [switch]$SkipDotNet,
    [switch]$SkipRegression,
    [switch]$FinalQa
)

$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runId = [guid]::NewGuid().ToString('N')
$databaseName = 'HospitalPhase7Ui_' + $runId
$phase = if ($FinalQa) { 'phase8' } else { 'phase7' }
$artifactDir = Join-Path $workspace "tests/browser/artifacts/$phase/$runId"
$apiProcess = $null
$webProcess = $null
$databaseCreated = $false
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
Write-Output "$phase evidence: $artifactDir"

function Invoke-Checked([string]$Name, [string]$Executable, [string[]]$Arguments, [string]$Directory = $workspace) {
    $log = Join-Path $artifactDir "$Name.log"
    Write-Output "Running $Name"
    Push-Location $Directory
    try {
        # Native tools may write diagnostics to stderr even when successful.
        $previousPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        & $Executable @Arguments *> $log
        $result = $LASTEXITCODE
        $ErrorActionPreference = $previousPreference
        if ($result -ne 0) { Get-Content -LiteralPath $log -Tail 50; throw "$Name failed with exit code $result. See $log" }
        Get-Content -LiteralPath $log -Tail 5
    }
    finally { Pop-Location }
}
function Wait-Ready([string]$Url, [System.Diagnostics.Process]$Process) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 90) {
        if ($Process.HasExited) { throw "Service exited before readiness: $Url" }
        try { $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2; if ($response.StatusCode -eq 200) { return } } catch { }
        Start-Sleep -Milliseconds 500
    }
    throw "Service readiness timed out: $Url"
}
function Invoke-Sql([string]$Name, [string]$Query) {
    Invoke-Checked $Name 'sqlcmd' @('-S', $SqlServer, '-E', '-C', '-b', '-Q', $Query)
}
function Invoke-DesktopRegression {
    Write-Output 'Running desktop-regression (120-second deadline)'
    $log = Join-Path $artifactDir 'desktop-regression.log'
    $errorLog = Join-Path $artifactDir 'desktop-regression-error.log'
    $desktopProcess = Start-Process dotnet -ArgumentList @('tests/Hospital.DesktopSmoke/bin/Debug/net9.0-windows/Hospital.DesktopSmoke.dll') -WorkingDirectory $workspace -WindowStyle Hidden -PassThru -RedirectStandardOutput $log -RedirectStandardError $errorLog
    # Retain the native handle so Windows PowerShell can read ExitCode after the child exits.
    $null = $desktopProcess.Handle
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        while (!$desktopProcess.WaitForExit(1000)) {
            if ($timer.Elapsed.TotalSeconds -ge 120) { throw "Desktop smoke exceeded 120 seconds. See $log" }
        }
        $desktopProcess.WaitForExit()
        Get-Content -LiteralPath $log -Tail 5
        if ($desktopProcess.ExitCode -ne 0) { Get-Content -LiteralPath $errorLog -Tail 30; throw 'Desktop regression failed.' }
    }
    finally {
        if (!$desktopProcess.HasExited) { Stop-Process -Id $desktopProcess.Id; $desktopProcess.WaitForExit(10000) | Out-Null }
        $desktopProcess.Dispose()
    }
}

try {
    Get-Command dotnet, node, python, sqlcmd -ErrorAction Stop | Out-Null
    if (!(Test-Path (Join-Path $workspace 'tests/browser/node_modules/playwright/cli.js'))) { throw 'Run npm ci and install Chromium in tests/browser first.' }
    foreach ($port in @($ApiPort, $WebPort)) {
        if (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) { throw "Port $port is already in use. Choose an unused port; existing services will not be stopped." }
    }
    if ($ApiPort -eq $WebPort) { throw 'API and Web ports must differ.' }
    if (!$SkipBuild) {
        Invoke-Checked 'build' 'dotnet' @('build', 'HospitalAppointmentSystem.sln', '--no-restore', '--no-incremental', '--disable-build-servers', '-m:1', '-v:minimal')
    }
    $env:HOSPITAL_TEST_SQLSERVER = "Server=$SqlServer;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
    if (!$SkipDotNet) {
        Invoke-Checked 'dotnet-tests' 'dotnet' @('test', 'HospitalAppointmentSystem.sln', '--no-build', '--no-restore', '-m:1', '--logger', 'trx;LogFileName=phase7.trx')
    }
    Invoke-Checked 'tokens' 'python' @('scripts/generate_design_tokens.py', '--check')
    Invoke-Sql 'create-database' "CREATE DATABASE [$databaseName];"
    $databaseCreated = $true
    $env:ConnectionStrings__DefaultConnection = "Server=$SqlServer;Database=$databaseName;Trusted_Connection=True;TrustServerCertificate=True;"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:Database__SeedDemo = 'true'
    $env:HOSPITAL_API_URL = "http://localhost:$ApiPort/"
    $env:Api__BaseUrl = $env:HOSPITAL_API_URL
    $env:HOSPITAL_WEB_URL = "http://localhost:$WebPort"
    $env:HOSPITAL_PHASE7_ISOLATED = 'true'
    $env:HOSPITAL_PHASE7_FIXTURE = Join-Path $artifactDir 'fixture.json'
    $apiProcess = Start-Process dotnet -ArgumentList @('bin/Debug/net9.0/Hospital.Api.dll', '--urls', $env:HOSPITAL_API_URL) -WorkingDirectory (Join-Path $workspace 'src/Hospital.Api') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $artifactDir 'api.log') -RedirectStandardError (Join-Path $artifactDir 'api-error.log')
    Wait-Ready ($env:HOSPITAL_API_URL + 'health/ready') $apiProcess
    $webProcess = Start-Process dotnet -ArgumentList @('bin/Debug/net9.0/Hospital.Web.dll', '--urls', $env:HOSPITAL_WEB_URL) -WorkingDirectory (Join-Path $workspace 'src/Hospital.Web') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $artifactDir 'web.log') -RedirectStandardError (Join-Path $artifactDir 'web-error.log')
    Wait-Ready ($env:HOSPITAL_WEB_URL + '/Account/Login') $webProcess
    Invoke-Checked 'phase7-browser' 'node' @('node_modules/playwright/cli.js', 'test', 'phase7.spec.js') (Join-Path $workspace 'tests/browser')

    # Parse identifiers as GUIDs before constructing SQL; no free-text fixture values enter a query.
    $proof = Get-Content -LiteralPath (Join-Path $artifactDir 'result.json') -Raw | ConvertFrom-Json
    $doctorId = ([guid]$proof.doctorId).ToString()
    $todayId = ([guid]$proof.todayAppointmentId).ToString()
    $lifecycleId = ([guid]$proof.lifecycleId).ToString()
    $raceId = ([guid]$proof.raceId).ToString()
    $closedSlot = ([guid]$proof.futureSlots[0]).ToString()
    $query = @"
USE [$databaseName];
IF (SELECT COUNT(*) FROM Appointments WHERE DoctorId = '$doctorId') <> 3 THROW 51000, 'Expected exactly three appointments, including one race winner and one idempotent lifecycle booking.', 1;
IF NOT EXISTS (SELECT 1 FROM Appointments WHERE Id = '$todayId' AND Status = 2 AND CheckedInAt IS NOT NULL) THROW 51000, 'Reception was not persisted.', 1;
IF (SELECT COUNT(*) FROM Appointments WHERE Id IN ('$lifecycleId', '$raceId') AND Status = 1) <> 2 THROW 51000, 'Cancellations were not persisted.', 1;
IF (SELECT COUNT(*) FROM AppointmentStatusHistories WHERE AppointmentId = '$lifecycleId') <> 3 THROW 51000, 'Lifecycle history contains duplicates or missing events.', 1;
IF (SELECT COUNT(*) FROM AppointmentStatusHistories WHERE AppointmentId = '$raceId') <> 2 THROW 51000, 'Race history contains duplicates.', 1;
IF (SELECT COUNT(*) FROM Notifications WHERE AppointmentId = '$lifecycleId') <> 3 THROW 51000, 'Missing or duplicate lifecycle notifications.', 1;
IF (SELECT COUNT(*) FROM Notifications WHERE AppointmentId = '$lifecycleId' AND IsRead = 1) <> 1 THROW 51000, 'Read notification state was not persisted.', 1;
-- TimeSlotStatus.Closed = 1; AppointmentStatus.CheckedIn = 2 is a different enum.
IF NOT EXISTS (SELECT 1 FROM TimeSlots WHERE Id = '$closedSlot' AND Status = 1 AND IsActive = 0) THROW 51000, 'Desktop closure was not persisted.', 1;
IF EXISTS (SELECT TimeSlotId FROM Appointments WHERE Status IN (0,2) GROUP BY TimeSlotId HAVING COUNT(*) > 1) THROW 51000, 'Multiple live bookings occupy a slot.', 1;
SELECT 'Phase7 SQL evidence passed' AS Result, (SELECT COUNT(*) FROM Appointments WHERE DoctorId = '$doctorId') AS Appointments, (SELECT COUNT(*) FROM Notifications WHERE AppointmentId = '$lifecycleId') AS LifecycleNotifications;
"@
    Invoke-Sql 'sql-evidence' $query
    if (!$SkipRegression) {
        Write-Output 'Waiting 60 seconds for the normal authentication rate-limit window before browser regression.'
        Start-Sleep -Seconds 60
        Invoke-Checked 'browser-regression' 'node' @('node_modules/playwright/cli.js', 'test', 'portal.spec.js', 'phase5.spec.js') (Join-Path $workspace 'tests/browser')
        if ($FinalQa) {
            Write-Output 'Waiting 60 seconds before final UI review.'
            Start-Sleep -Seconds 60
            $env:HOSPITAL_PHASE8_OUTPUT = Join-Path $artifactDir 'final-ui'
            Invoke-Checked 'final-ui' 'node' @('node_modules/playwright/cli.js', 'test', 'phase8.spec.js') (Join-Path $workspace 'tests/browser')
        }
        Write-Output 'Waiting 60 seconds for the normal authentication rate-limit window before Desktop regression.'
        Start-Sleep -Seconds 60
        Invoke-DesktopRegression
    }
    Write-Output "$phase verification passed."
}
finally {
    foreach ($process in @($webProcess, $apiProcess)) {
        if ($null -ne $process -and !$process.HasExited) { Stop-Process -Id $process.Id -ErrorAction Continue; $process.WaitForExit(10000) | Out-Null }
    }
    if ($databaseCreated) {
        if ($databaseName -notmatch '^HospitalPhase7Ui_[a-f0-9]{32}$') { throw 'Unexpected cleanup database name.' }
        Invoke-Sql 'cleanup' "IF DB_ID(N'$databaseName') IS NOT NULL BEGIN ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]; END; SELECT COUNT(*) AS RemainingTestDatabase FROM sys.databases WHERE name = N'$databaseName';"
    }
    Write-Output "Evidence retained at $artifactDir"
}
