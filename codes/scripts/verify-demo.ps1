param([string]$SqlServer = '.\SQLEXPRESS', [int]$ApiPort = 5038, [int]$WebPort = 5178)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$run = [guid]::NewGuid().ToString('N')
$database = 'HospitalPhase8Demo_' + $run
$state = "tmp/phase8-demo/$run"
$staleState = "$state/stale"
$previousConnection = $env:ConnectionStrings__DefaultConnection
$env:ConnectionStrings__DefaultConnection = "Server=$SqlServer;Database=$database;Trusted_Connection=True;TrustServerCertificate=True;"
function Demo([string]$Action, [string]$Directory = $state, [bool]$ShouldPass = $true) {
    New-Item -ItemType Directory -Path $state -Force | Out-Null
    $log = Join-Path $state ("$Action-" + [guid]::NewGuid().ToString('N') + '.log')
    # Wait for the launcher only; redirected native pipelines can keep waiting for its services.
    $launcher = Start-Process powershell -ArgumentList @('-NoProfile', '-File', 'scripts/demo.ps1', '-Action', $Action, '-SkipBuild', '-SqlServer', $SqlServer, '-DatabaseName', $database, '-ApiPort', $ApiPort, '-WebPort', $WebPort, '-StateDirectory', $Directory) -WindowStyle Hidden -PassThru -RedirectStandardOutput $log -RedirectStandardError ($log + '.error')
    $null = $launcher.Handle
    try {
        if (!$launcher.WaitForExit(120000)) { Stop-Process -Id $launcher.Id; throw "Demo launcher timed out: $Action" }
        $result = $launcher.ExitCode
        if (($result -eq 0) -ne $ShouldPass) { Get-Content -LiteralPath ($log + '.error') -Tail 20; throw "Unexpected demo.ps1 result for $Action" }
    } finally { $launcher.Dispose() }
    Write-Output "Verified demo $Action (exit $result). Log: $log"
}
function Snapshot {
    $connection = New-Object System.Data.SqlClient.SqlConnection $env:ConnectionStrings__DefaultConnection
    try {
        $connection.Open(); $command = $connection.CreateCommand()
        $command.CommandText = 'SELECT Id FROM Users ORDER BY Id; SELECT Id FROM Appointments ORDER BY Id;'
        $reader = $command.ExecuteReader(); $ids = @()
        do { while ($reader.Read()) { $ids += $reader.GetGuid(0).ToString() } } while ($reader.NextResult())
        $reader.Dispose(); return ($ids -join ',')
    } finally { $connection.Dispose() }
}
Push-Location $workspace
try {
    Demo 'start'; Demo 'status'
    $before = Snapshot
    if ($before.Split(',').Count -ne 8) { throw 'Expected six seeded users and two appointments.' }
    # Duplicate starts must leave the first session running.
    Demo 'start' $state $false
    if ((Invoke-WebRequest "http://localhost:$ApiPort/health/ready" -UseBasicParsing).StatusCode -ne 200) { throw 'Duplicate start interrupted API.' }
    # A recycled PID with a mismatched start time must never be stopped.
    $saved = Get-Content -LiteralPath (Join-Path $state 'session.json') -Raw | ConvertFrom-Json
    foreach ($entry in $saved.processes) { $entry.startedUtc = '2000-01-01T00:00:00.0000000Z' }
    New-Item -ItemType Directory -Path $staleState -Force | Out-Null
    $saved | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $staleState 'session.json') -Encoding UTF8
    Demo 'stop' $staleState
    if ((Invoke-WebRequest "http://localhost:$ApiPort/health/ready" -UseBasicParsing).StatusCode -ne 200) { throw 'Stale state interrupted API.' }
    Demo 'stop'; Demo 'start'
    if ((Snapshot) -ne $before) { throw 'Restart changed seeded identities or appointment records.' }
    Demo 'status'; Demo 'stop'
    Write-Output 'Demo lifecycle passed: start/readiness/status, duplicate-start guard, stale-PID guard, stop/restart and retained database identities.'
}
finally {
    try { Demo 'stop' } finally {
        if ($database -notmatch '^HospitalPhase8Demo_[a-f0-9]{32}$') { throw 'Unexpected cleanup database.' }
        $master = New-Object System.Data.SqlClient.SqlConnection "Server=$SqlServer;Database=master;Integrated Security=true;TrustServerCertificate=true"
        try {
            $master.Open(); $command = $master.CreateCommand()
            $command.CommandText = "IF DB_ID(N'$database') IS NOT NULL BEGIN ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database]; END;"
            $null = $command.ExecuteNonQuery(); Write-Output "Removed owned test database: $database"
        } finally { $master.Dispose(); $env:ConnectionStrings__DefaultConnection = $previousConnection; Pop-Location }
    }
}
