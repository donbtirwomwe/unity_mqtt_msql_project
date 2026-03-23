$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "Created containers/.env from template. Update password/ports if needed."
}

$envMap = @{}
Get-Content ".env" | ForEach-Object {
    $line = $_.Trim()
    if (-not $line -or $line.StartsWith("#")) { return }
    $parts = $line.Split("=", 2)
    if ($parts.Length -eq 2) {
        $envMap[$parts[0].Trim()] = $parts[1].Trim()
    }
}

$mqttUser = $envMap["MQTT_APP_USERNAME"]
$mqttPassword = $envMap["MQTT_APP_PASSWORD"]
$mqttTopicPrefix = if ($envMap.ContainsKey("MQTT_APP_TOPIC_PREFIX")) { $envMap["MQTT_APP_TOPIC_PREFIX"] } else { "mx" }
$sqlServerHost = if ($envMap.ContainsKey("APP_DB_HOST")) { $envMap["APP_DB_HOST"] } elseif ($envMap.ContainsKey("DB_SERVER_IP")) { $envMap["DB_SERVER_IP"] } else { "127.0.0.1" }
$sqlServerPort = if ($envMap.ContainsKey("DB_PORT")) { $envMap["DB_PORT"] } else { "1433" }
$sqlDatabase = if ($envMap.ContainsKey("SQL_DATABASE")) { $envMap["SQL_DATABASE"] } else { "IndustrialAssets" }
$sqlUser = $envMap["APP_DB_USER"]
$sqlPassword = $envMap["APP_DB_PASSWORD"]
$mqttAclDbUser = if ($envMap.ContainsKey("MQTT_ACL_DB_USER")) { $envMap["MQTT_ACL_DB_USER"] } elseif ($envMap.ContainsKey("APP_DB_USER")) { $envMap["APP_DB_USER"] } else { "app_reader" }
$useLocalSqlContainer = $sqlServerHost -in @("127.0.0.1", "localhost", ".")

if ([string]::IsNullOrWhiteSpace($mqttUser) -or [string]::IsNullOrWhiteSpace($mqttPassword)) {
    throw "MQTT_APP_USERNAME and MQTT_APP_PASSWORD must be set in containers/.env before starting the broker."
}

if ([string]::IsNullOrWhiteSpace($sqlUser) -or [string]::IsNullOrWhiteSpace($sqlPassword)) {
    throw "APP_DB_USER and APP_DB_PASSWORD must be set in containers/.env so MQTT ACLs can be generated from SQL."
}

if ($useLocalSqlContainer) {
    docker compose --env-file .env -f docker-compose.yml up -d mssql

    $sqlReady = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            $probeConn = New-Object System.Data.SqlClient.SqlConnection
            $probeConn.ConnectionString = "Server=$sqlServerHost,$sqlServerPort;Database=$sqlDatabase;User Id=$sqlUser;Password=$sqlPassword;TrustServerCertificate=True;"
            $probeConn.Open()
            $probeConn.Close()
            $sqlReady = $true
            break
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $sqlReady) {
        throw "Local SQL container did not become ready in time."
    }
}

$authDir = Join-Path $PSScriptRoot "mqtt\auth"
New-Item -ItemType Directory -Force -Path $authDir | Out-Null

$aclFile = Join-Path $authDir "acl"

$topicRules = New-Object System.Collections.Generic.List[string]

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = "Server=$sqlServerHost,$sqlServerPort;Database=$sqlDatabase;User Id=$sqlUser;Password=$sqlPassword;TrustServerCertificate=True;"
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "dbo.usp_GetMqttAclEntries"
    $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
    $null = $cmd.Parameters.AddWithValue("@userId", $mqttAclDbUser)

    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        $accessMode = $reader["AccessMode"].ToString().Trim().ToLowerInvariant()
        $topicPath = $reader["TopicPath"].ToString().Trim()
        if (-not [string]::IsNullOrWhiteSpace($accessMode) -and -not [string]::IsNullOrWhiteSpace($topicPath)) {
            $topicRules.Add("topic $accessMode $topicPath")
        }
    }
    $reader.Close()
    $conn.Close()
}
catch {
    throw "Failed to build MQTT ACL from SQL: $($_.Exception.Message)"
}

if ($topicRules.Count -eq 0) {
    throw "No MQTT ACL rules were returned from SQL. Refusing to start broker with empty ACL."
}

$aclLines = New-Object System.Collections.Generic.List[string]
$aclLines.Add("user $mqttUser")
foreach ($rule in ($topicRules | Sort-Object -Unique)) {
    $aclLines.Add($rule)
}

Set-Content -Path $aclFile -Value $aclLines -Encoding ascii

$authDirResolved = (Resolve-Path $authDir).Path
docker run --rm -v "${authDirResolved}:/work" eclipse-mosquitto:2 mosquitto_passwd -b -c /work/passwd "$mqttUser" "$mqttPassword" | Out-Null

if ($useLocalSqlContainer) {
    docker compose --env-file .env -f docker-compose.yml up -d --force-recreate mqtt
}
else {
    docker compose --env-file .env -f docker-compose.yml up -d --force-recreate
}
Write-Host "Containers started."
