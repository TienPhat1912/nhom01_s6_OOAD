function Get-DemoConfiguration {
    param([Parameter(Mandatory=$true)][string]$Workspace, [string]$ConfigPath = 'demo.local.json')
    $root = [IO.Path]::GetFullPath($Workspace).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $path = if ([IO.Path]::IsPathRooted($ConfigPath)) { [IO.Path]::GetFullPath($ConfigPath) } else { [IO.Path]::GetFullPath((Join-Path $root $ConfigPath)) }
    if (!$path.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'ConfigPath must stay inside codes.'
    }
    $settings = @{ SqlServer='.\SQLEXPRESS'; DatabaseName='HospitalDemo'; ApiPort=5014; WebPort=5174; ConnectionString='' }
    if (Test-Path -LiteralPath $path) {
        try { $config = [IO.File]::ReadAllText($path) | ConvertFrom-Json } catch { throw 'Invalid JSON in the demo configuration file.' }
        if ($null -eq $config -or $config -isnot [pscustomobject]) { throw 'Demo configuration must be a JSON object.' }
        foreach ($property in $config.PSObject.Properties) {
            if (!$settings.ContainsKey($property.Name)) { throw "Unknown demo setting: $($property.Name)" }
            $settings[$property.Name] = $property.Value
        }
    }
    if ([string]::IsNullOrWhiteSpace($settings.SqlServer)) { throw 'SqlServer must not be empty.' }
    if ($settings.DatabaseName -notmatch '^[A-Za-z][A-Za-z0-9_]{0,100}$') { throw 'Invalid DatabaseName.' }
    foreach ($key in @('ApiPort', 'WebPort')) {
        if ([string]$settings[$key] -notmatch '^[0-9]{4,5}$' -or [int]$settings[$key] -lt 1024 -or [int]$settings[$key] -gt 65535) {
            throw "Invalid $key. Use a port between 1024 and 65535."
        }
        $settings[$key] = [int]$settings[$key]
    }
    if ($settings.ApiPort -eq $settings.WebPort) { throw 'API and Web ports must differ.' }
    if ($null -ne $settings.ConnectionString -and $settings.ConnectionString -isnot [string]) { throw 'ConnectionString must be a string.' }
    return $settings
}
