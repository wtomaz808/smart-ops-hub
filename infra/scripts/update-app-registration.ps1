#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Updates the Entra ID app registration with the deployed web app FQDN redirect URI.

.DESCRIPTION
    Called after infrastructure deployment to add the Container App FQDN as a redirect URI.

.PARAMETER AppId
    The app (client) ID of the registered application.

.PARAMETER WebAppFqdn
    FQDN of the deployed web container app.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$AppId,

    [Parameter(Mandatory)]
    [string]$WebAppFqdn
)

$ErrorActionPreference = 'Stop'

Write-Host "Updating redirect URIs for app $AppId..." -ForegroundColor Yellow

# Get current redirect URIs
$app = az ad app show --id $AppId -o json | ConvertFrom-Json
$currentUris = @($app.web.redirectUris)

$newUri = "https://$WebAppFqdn/signin-oidc"
$logoutUri = "https://$WebAppFqdn/signout-oidc"

if ($currentUris -contains $newUri) {
    Write-Host "  Redirect URI already configured: $newUri" -ForegroundColor Green
}
else {
    $currentUris += $newUri
    $urisJson = ($currentUris | ConvertTo-Json -Compress)

    az ad app update --id $AppId --web-redirect-uris @($currentUris) --output none

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Added redirect URI: $newUri" -ForegroundColor Green
    }
    else {
        Write-Error "  Failed to update redirect URIs."
    }
}

# Set front-channel logout URL
az ad app update --id $AppId --set web.logoutUrl=$logoutUri --output none 2>$null
Write-Host "  Logout URI set: $logoutUri" -ForegroundColor Green

Write-Host "  App registration updated successfully." -ForegroundColor Green
