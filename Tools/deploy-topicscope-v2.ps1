# ==============================================================================
# TOPIC INDIRECTION DEPLOYMENT GUIDE
# ==============================================================================
# Two-step process:
# 1. SA runs grant script (one-time)
# 2. app_reader self-executes schema script
# ==============================================================================

param(
    [string]$ServerIp = "192.168.1.50",
    [int]$Port = 1433,
    [string]$Database = "IndustrialAssets",
    [string]$EnvFilePath = "..\containers\.env"
)

function Invoke-SqlFileBatches {
    param(
        [Parameter(Mandatory=$true)]
        [System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory=$true)]
        [string]$SqlFilePath
    )

    $content = Get-Content $SqlFilePath -Raw
    $lines = $content -split "`r?`n"
    $currentBatch = New-Object System.Collections.Generic.List[string]

    foreach ($line in $lines) {
        if ($line -match '^\s*GO\s*$') {
            if ($currentBatch.Count -gt 0) {
                $cmd = $Connection.CreateCommand()
                $cmd.CommandText = ($currentBatch -join [Environment]::NewLine)
                $cmd.CommandTimeout = 60
                $cmd.ExecuteNonQuery() | Out-Null
                $currentBatch.Clear()
            }
            continue
        }

        $currentBatch.Add($line)
    }

    if ($currentBatch.Count -gt 0) {
        $cmd = $Connection.CreateCommand()
        $cmd.CommandText = ($currentBatch -join [Environment]::NewLine)
        $cmd.CommandTimeout = 60
        $cmd.ExecuteNonQuery() | Out-Null
    }
}

# Load credentials from .env file
if (-not (Test-Path $EnvFilePath)) {
    Write-Host "ERROR: .env file not found at $EnvFilePath" -ForegroundColor Red
    exit 1
}

$envVars = @{}
Get-Content $EnvFilePath | ForEach-Object {
    if ($_ -match '^\s*([^=]+)=(.+)$') {
        $envVars[$matches[1]] = $matches[2]
    }
}

$SaPassword = $envVars['SA_PASSWORD']
$AppReaderPassword = $envVars['APP_DB_PASSWORD']

if (-not $SaPassword -or -not $AppReaderPassword) {
    Write-Host "ERROR: SA_PASSWORD or APP_DB_PASSWORD not found in .env file" -ForegroundColor Red
    exit 1
}

$VerbosePreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Topic Indirection v2 Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# STEP 1: SA grants schema rights to app_reader
Write-Host "STEP 1: Granting schema rights to app_reader..." -ForegroundColor Yellow

$grantScript = Get-Content ".\grant-app-reader-schema-rights.sql" -Raw
$grantScript = $grantScript -replace '__APP_DB_USER__', 'app_reader'

try {
    $saConn = New-Object System.Data.SqlClient.SqlConnection
    $saConn.ConnectionString = "Server=$ServerIp,$Port;Database=$Database;User Id=sa;Password=$SaPassword;TrustServerCertificate=True;"
    $saConn.Open()
    
    $saCmd = $saConn.CreateCommand()
    $saCmd.CommandText = $grantScript
    $saCmd.CommandTimeout = 30
    $saCmd.ExecuteNonQuery() | Out-Null
    
    Write-Host "[OK] Schema rights granted to app_reader" -ForegroundColor Green
    $saConn.Close()
}
catch {
    Write-Host "[ERROR] FAILED to grant schema rights: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# STEP 2: app_reader self-executes v2 schema
Write-Host "STEP 2: app_reader creating topic indirection tables/procedures..." -ForegroundColor Yellow

$v2ScriptPath = ".\apply-topicscope-v2-as-app_reader-fixed.sql"

try {
    $appConn = New-Object System.Data.SqlClient.SqlConnection
    $appConn.ConnectionString = "Server=$ServerIp,$Port;Database=$Database;User Id=app_reader;Password=$AppReaderPassword;TrustServerCertificate=True;"
    $appConn.Open()

    Invoke-SqlFileBatches -Connection $appConn -SqlFilePath $v2ScriptPath
    
    Write-Host "[OK] Topic indirection schema created successfully" -ForegroundColor Green
    $appConn.Close()
}
catch {
    Write-Host "[ERROR] FAILED to create schema: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  • TOPICMAPPINGS table created (channel -> TopicUID mapping)"
Write-Host "  • ASSETACCESS table created (user -> asset access control)"
Write-Host "  • All procedures updated to use TopicUID instead of raw topics"
Write-Host "  • New procedures: usp_GetTopicMapping, usp_GetAccessibleTopics, usp_GetAccessibleAssets"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  • Update Unity code to read TopicUID from procedures"
Write-Host "  • Test with GetDataPointChannels - should return uid_* format"
Write-Host "  • Configure MQTT broker auth and ACLs to validate TopicUID access"
