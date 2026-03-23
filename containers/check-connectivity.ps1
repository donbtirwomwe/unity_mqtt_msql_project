$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

$envFile = if (Test-Path ".env") { ".env" } else { ".env.example" }
$envMap = @{}

Get-Content $envFile | ForEach-Object {
    $line = $_.Trim()
    if (-not $line -or $line.StartsWith("#")) { return }
    $parts = $line.Split("=", 2)
    if ($parts.Length -eq 2) {
        $envMap[$parts[0].Trim()] = $parts[1].Trim()
    }
}

$saPassword = if ($envMap.ContainsKey("SA_PASSWORD")) { $envMap["SA_PASSWORD"] } else { "" }
$sqlDatabase = if ($envMap.ContainsKey("SQL_DATABASE")) { $envMap["SQL_DATABASE"] } else { "IndustrialAssets" }
$appDbUser = if ($envMap.ContainsKey("APP_DB_USER")) { $envMap["APP_DB_USER"] } else { "app_reader" }
$appDbPassword = if ($envMap.ContainsKey("APP_DB_PASSWORD")) { $envMap["APP_DB_PASSWORD"] } else { "" }
$mqttAppUser = if ($envMap.ContainsKey("MQTT_APP_USERNAME")) { $envMap["MQTT_APP_USERNAME"] } else { "" }
$mqttTopicPrefix = if ($envMap.ContainsKey("MQTT_APP_TOPIC_PREFIX")) { $envMap["MQTT_APP_TOPIC_PREFIX"] } else { "mx" }
$mqttPort = if ($envMap.ContainsKey("MQTT_HOST_PORT")) { [int]$envMap["MQTT_HOST_PORT"] } else { 1884 }

Write-Host "Using env file: $envFile"

if ([string]::IsNullOrWhiteSpace($saPassword)) {
    throw "SA_PASSWORD must be set in $envFile"
}

# 1) Check Docker is available.
docker version | Out-Null

# 2) Check container runtime state.
$containers = docker compose --env-file $envFile -f docker-compose.yml ps --services --status running
$required = @("mssql", "mqtt")
$missing = @()
foreach ($svc in $required) {
    if ($containers -notcontains $svc) {
        $missing += $svc
    }
}

if ($missing.Count -gt 0) {
    throw "Required containers not running: $($missing -join ', '). Start with: pwsh ./containers/up.ps1"
}

# 3) Check network ports.
$sqlPortOpen = Test-NetConnection -ComputerName "127.0.0.1" -Port 1433 -WarningAction SilentlyContinue
if (-not $sqlPortOpen.TcpTestSucceeded) {
    throw "SQL port check failed on 127.0.0.1:1433"
}

$mqttPortOpen = Test-NetConnection -ComputerName "127.0.0.1" -Port $mqttPort -WarningAction SilentlyContinue
if (-not $mqttPortOpen.TcpTestSucceeded) {
    throw "MQTT port check failed on 127.0.0.1:$mqttPort"
}

# 4) Verify SQL login + DB exists from inside the SQL container.
$null = docker exec industrial-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$saPassword" -No -Q "SELECT 1"
$null = docker exec industrial-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$saPassword" -No -Q "IF DB_ID(N'$sqlDatabase') IS NULL THROW 50000, 'Database missing', 1; SELECT DB_NAME(DB_ID(N'$sqlDatabase'));"

# 5) Verify MQTT broker process is running in container.
$procCheck = docker exec industrial-mqtt sh -lc "ps aux | grep mosquitto | grep -v grep"
if (-not $procCheck) {
    throw "MQTT broker process check failed in industrial-mqtt container"
}

# 6) Verify app DB login and MQTT auth files exist.
if (-not [string]::IsNullOrWhiteSpace($appDbPassword)) {
    $null = docker exec industrial-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U "$appDbUser" -P "$appDbPassword" -No -d "$sqlDatabase" -Q "EXEC dbo.usp_GetAssetCount"
}

$authCheck = docker exec industrial-mqtt sh -lc "test -s /mosquitto/config/auth/passwd && test -s /mosquitto/config/auth/acl && grep -q '^user $mqttAppUser$' /mosquitto/config/auth/acl && grep -q '^topic readwrite $mqttTopicPrefix/#$' /mosquitto/config/auth/acl"
if ($LASTEXITCODE -ne 0) {
    throw "MQTT auth file check failed in industrial-mqtt container"
}

Write-Host "Connectivity checks passed: SQL + MQTT are reachable and initialized."
