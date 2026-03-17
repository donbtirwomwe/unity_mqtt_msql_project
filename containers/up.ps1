$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "Created containers/.env from template. Update password/ports if needed."
}

docker compose --env-file .env -f docker-compose.yml up -d --build
Write-Host "Containers started."
