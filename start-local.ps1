<#
.SYNOPSIS
    Starts the Smart Ops Hub API and Web servers locally for development.

.DESCRIPTION
    Launches both the API (port 5100) and Web (port 5050) servers in separate
    console windows. Press Ctrl+C in either window to stop that server.

.EXAMPLE
    .\start-local.ps1            # Build first, then start both servers
    .\start-local.ps1 -NoBuild   # Skip build (use last compiled output)
#>
param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# Build unless -NoBuild
if (-not $NoBuild) {
    Write-Host "Building solution..." -ForegroundColor Cyan
    dotnet build "$root\smart-ops-hub.sln" --verbosity quiet
    if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; return }
    Write-Host "Build succeeded.`n" -ForegroundColor Green
}

Write-Host "Starting API  -> http://localhost:5100" -ForegroundColor Yellow
Write-Host "Starting Web  -> http://localhost:5050" -ForegroundColor Yellow
Write-Host "Starting Admin-> http://localhost:5050/admin" -ForegroundColor Yellow
Write-Host "`nClose this window or press Ctrl+C to stop both servers.`n" -ForegroundColor Gray

$env:ASPNETCORE_ENVIRONMENT = "Development"

# Start API in a new visible console window
$api = Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "`$env:ASPNETCORE_ENVIRONMENT='Development'; Set-Location '$root\src\SmartOpsHub.Api'; dotnet run --no-build --urls http://localhost:5100"
) -PassThru

# Start Web in a new visible console window
$web = Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "`$env:ASPNETCORE_ENVIRONMENT='Development'; Set-Location '$root\src\SmartOpsHub.Web'; dotnet run --no-build --urls http://localhost:5050"
) -PassThru

Write-Host "API PID: $($api.Id)   Web PID: $($web.Id)" -ForegroundColor Cyan
Write-Host "`nServers starting in separate windows. Wait a few seconds then open:" -ForegroundColor Green
Write-Host "  Web UI:    http://localhost:5050"
Write-Host "  Admin:     http://localhost:5050/admin"
Write-Host "  API Health:http://localhost:5100/health"
Write-Host "`nTo stop: close the server windows, or run:" -ForegroundColor Gray
Write-Host "  Stop-Process -Id $($api.Id),$($web.Id)" -ForegroundColor Gray
