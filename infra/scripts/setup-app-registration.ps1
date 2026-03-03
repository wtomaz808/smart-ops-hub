#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates or updates the Entra ID app registration for AgentOpsHub site authentication.

.DESCRIPTION
    Registers an app in Entra ID for MSAL-based user authentication.
    Creates a service principal if one doesn't exist.
    Configures redirect URIs, ID tokens, and Microsoft Graph User.Read permission.

.PARAMETER Environment
    Target environment: dev or prod.

.PARAMETER TenantId
    Azure AD tenant ID.

.PARAMETER WebAppFqdn
    (Optional) FQDN of the deployed web app. If not provided, redirect URIs are set
    after infrastructure deployment using update-app-registration.ps1.

.EXAMPLE
    ./setup-app-registration.ps1 -Environment dev -TenantId "d14ab12e-..."
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory)]
    [string]$TenantId,

    [string]$WebAppFqdn
)

$ErrorActionPreference = 'Stop'

$AppDisplayName = "AgentOpsHub-$Environment"
# Azure Government Entra ID endpoints
$Instance = "https://login.microsoftonline.us/"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  AgentOpsHub — App Registration Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  App Name    : $AppDisplayName"
Write-Host "  Tenant ID   : $TenantId"
Write-Host "  Instance    : $Instance"
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if app registration already exists
Write-Host "[1/4] Checking for existing app registration..." -ForegroundColor Yellow
$existingApp = az ad app list --display-name $AppDisplayName --query "[0]" -o json 2>$null | ConvertFrom-Json

if ($existingApp) {
    $AppId = $existingApp.appId
    $ObjectId = $existingApp.id
    Write-Host "  Found existing app: $AppId" -ForegroundColor Green
}
else {
    # Create new app registration
    Write-Host "[2/4] Creating app registration..." -ForegroundColor Yellow

    # Build redirect URIs
    $redirectUris = @("https://localhost:5050/signin-oidc")
    if ($WebAppFqdn) {
        $redirectUris += "https://$WebAppFqdn/signin-oidc"
    }

    $webConfig = @{
        redirectUris          = $redirectUris
        implicitGrantSettings = @{
            enableIdTokenIssuance = $true
        }
    }

    $appBody = @{
        displayName = $AppDisplayName
        signInAudience = "AzureADMyOrg"
        web = $webConfig
        requiredResourceAccess = @(
            @{
                resourceAppId = "00000003-0000-0000-c000-000000000000"  # Microsoft Graph
                resourceAccess = @(
                    @{
                        id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d"  # User.Read
                        type = "Scope"
                    }
                )
            }
        )
    } | ConvertTo-Json -Depth 5

    $newApp = $appBody | az ad app create --body @- -o json | ConvertFrom-Json

    if ($LASTEXITCODE -ne 0 -or -not $newApp) {
        Write-Error "Failed to create app registration."
    }

    $AppId = $newApp.appId
    $ObjectId = $newApp.id
    Write-Host "  Created app: $AppId (Object ID: $ObjectId)" -ForegroundColor Green
}

# Ensure service principal exists
Write-Host "[3/4] Ensuring service principal exists..." -ForegroundColor Yellow
$sp = az ad sp show --id $AppId -o json 2>$null | ConvertFrom-Json
if (-not $sp) {
    az ad sp create --id $AppId --output none
    Write-Host "  Service principal created." -ForegroundColor Green
}
else {
    Write-Host "  Service principal already exists." -ForegroundColor Green
}

# Create client secret for the web app
Write-Host "[4/4] Creating client secret..." -ForegroundColor Yellow
$secret = az ad app credential reset --id $AppId --display-name "$AppDisplayName-secret" --years 2 --query password -o tsv

if ($LASTEXITCODE -ne 0) {
    Write-Warning "  Could not create client secret. You may need to create one manually."
    $secret = $null
}
else {
    Write-Host "  Client secret created (store securely!)." -ForegroundColor Green
}

# Output summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  App Registration Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  App (Client) ID : $AppId"
Write-Host "  Object ID       : $ObjectId"
Write-Host "  Tenant ID       : $TenantId"
Write-Host "  Instance         : $Instance"
if ($secret) {
    Write-Host "  Client Secret   : $secret" -ForegroundColor Yellow
    Write-Host "  ⚠ Save this secret — it won't be shown again!" -ForegroundColor Red
}
Write-Host "========================================`n" -ForegroundColor Cyan

# Return values for use by deploy.ps1
return @{
    AppId    = $AppId
    ObjectId = $ObjectId
    TenantId = $TenantId
    Instance = $Instance
    Secret   = $secret
}
