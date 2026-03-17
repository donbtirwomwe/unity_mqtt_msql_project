$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

docker compose --env-file .env -f docker-compose.yml down
Write-Host "Containers stopped."
