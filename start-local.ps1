<#
.SYNOPSIS
    Starts the AgentOpsHub locally for development.

.DESCRIPTION
    Two modes:
      -Docker   Runs all services (Web, API, MCP Gateway, SQL Server) via
                docker compose. This is the recommended approach.
      (default) Launches API and Web as dotnet processes in separate console
                windows (no SQL Server or MCP Gateway).

.EXAMPLE
    .\start-local.ps1              # dotnet run (build + launch)
    .\start-local.ps1 -NoBuild     # dotnet run (skip build)
    .\start-local.ps1 -Docker      # docker compose up
    .\start-local.ps1 -Docker -Build  # docker compose up --build
    .\start-local.ps1 -Down        # docker compose down (stop containers)
#>
param(
    [switch]$Docker,
    [switch]$Down,
    [switch]$Build,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# --- Docker compose down ---
if ($Down) {
    Write-Host "Stopping containers..." -ForegroundColor Cyan
    docker compose -f "$root\docker-compose.yml" down
    return
}

# --- Docker mode ---
if ($Docker) {
    Write-Host "Starting AgentOpsHub via Docker Compose..." -ForegroundColor Cyan

    $composeArgs = @("-f", "$root\docker-compose.yml", "up", "-d")
    if ($Build) { $composeArgs += "--build" }

    docker compose @composeArgs
    if ($LASTEXITCODE -ne 0) { Write-Error "docker compose up failed"; return }

    Write-Host "`nContainers starting. Wait a few seconds then open:" -ForegroundColor Green
    Write-Host "  Web UI:      http://localhost:5050"
    Write-Host "  Admin:       http://localhost:5050/admin"
    Write-Host "  API Health:  http://localhost:5100/health"
    Write-Host "  MCP Gateway: http://localhost:5200/health"
    Write-Host "  SQL Server:  localhost:1433"
    Write-Host "`nTo stop: .\start-local.ps1 -Down" -ForegroundColor Gray
    Write-Host "Logs:   docker compose logs -f" -ForegroundColor Gray
    return
}

# --- Dotnet mode (default) ---
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

$api = Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "`$env:ASPNETCORE_ENVIRONMENT='Development'; Set-Location '$root\src\SmartOpsHub.Api'; dotnet run --no-build --urls http://localhost:5100"
) -PassThru

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
