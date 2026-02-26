#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys Smart Ops Hub infrastructure to Azure Government using Bicep.

.DESCRIPTION
    Provisions all Azure resources (Container Apps, ACR, SQL, OpenAI, etc.)
    from the Bicep templates. Supports dev/prod environments, what-if preview,
    and outputs the values needed for GitHub Actions secrets/vars.

.PARAMETER Environment
    Target environment: dev or prod.

.PARAMETER WhatIf
    Preview changes without deploying.

.PARAMETER Location
    Azure region. Defaults to usgovvirginia.

.PARAMETER ImageTag
    Container image tag. Defaults to 'latest'.

.EXAMPLE
    # Preview dev deployment
    ./deploy.ps1 -Environment dev -WhatIf

    # Deploy dev environment
    ./deploy.ps1 -Environment dev

    # Deploy prod with specific image tag
    ./deploy.ps1 -Environment prod -ImageTag abc123
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment,

    [switch]$WhatIf,

    [string]$Location = 'usgovvirginia',

    [string]$ImageTag = 'latest'
)

$ErrorActionPreference = 'Stop'

$ProjectName = 'smart-ops-hub'
$ResourceGroup = "$ProjectName-$Environment-rg"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$InfraDir = Split-Path -Parent $ScriptDir
$ParamFile = Join-Path $InfraDir "parameters/$Environment.bicepparam"
$BicepFile = Join-Path $InfraDir 'main.bicep'

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Smart Ops Hub — Infrastructure Deploy" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Environment : $Environment"
Write-Host "  Location    : $Location"
Write-Host "  Image Tag   : $ImageTag"
Write-Host "  Resource Grp: $ResourceGroup"
Write-Host "  Param File  : $ParamFile"
Write-Host "========================================`n" -ForegroundColor Cyan

# Verify Azure CLI is logged in and set to the right cloud
Write-Host "[1/4] Verifying Azure CLI session..." -ForegroundColor Yellow
$cloud = az cloud show --query name -o tsv 2>$null
if ($cloud -ne 'AzureUSGovernment') {
    Write-Host "  Switching to AzureUSGovernment cloud..." -ForegroundColor Yellow
    az cloud set --name AzureUSGovernment
}

$account = az account show --query '{name:name, id:id}' -o json 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged in to Azure CLI. Run 'az login --cloud AzureUSGovernment' first."
}
Write-Host "  Subscription: $($account.name) ($($account.id))" -ForegroundColor Green

# Create resource group if it doesn't exist
Write-Host "`n[2/4] Ensuring resource group exists..." -ForegroundColor Yellow
az group create --name $ResourceGroup --location $Location --tags environment=$Environment project=$ProjectName managedBy=bicep --output none
Write-Host "  Resource group '$ResourceGroup' ready." -ForegroundColor Green

# Validate / What-If / Deploy
if ($WhatIf) {
    Write-Host "`n[3/4] Running what-if preview..." -ForegroundColor Yellow
    az deployment group what-if `
        --resource-group $ResourceGroup `
        --template-file $BicepFile `
        --parameters $ParamFile `
        --parameters imageTag=$ImageTag
    Write-Host "`n  What-if complete. No changes were made." -ForegroundColor Green
}
else {
    Write-Host "`n[3/4] Deploying Bicep template..." -ForegroundColor Yellow
    $result = az deployment group create `
        --resource-group $ResourceGroup `
        --template-file $BicepFile `
        --parameters $ParamFile `
        --parameters imageTag=$ImageTag `
        --name "$ProjectName-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss')" `
        --output json | ConvertFrom-Json

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Deployment failed. Check the Azure portal for details."
    }

    Write-Host "  Deployment succeeded!" -ForegroundColor Green

    # Extract outputs
    Write-Host "`n[4/4] Deployment Outputs:" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Cyan
    $outputs = $result.properties.outputs
    Write-Host "  Web App FQDN      : $($outputs.webAppFqdn.value)"
    Write-Host "  API App FQDN      : $($outputs.apiAppFqdn.value)"
    Write-Host "  MCP Gateway FQDN  : $($outputs.mcpGatewayFqdn.value)"
    Write-Host "  OpenAI Endpoint   : $($outputs.openAiEndpoint.value)"
    Write-Host "  SQL Server FQDN   : $($outputs.sqlServerFqdn.value)"
    Write-Host "  Key Vault URI     : $($outputs.keyVaultUri.value)"
    Write-Host "  Identity Client ID: $($outputs.identityClientId.value)"
    Write-Host "========================================`n" -ForegroundColor Cyan

    # Output the GitHub Actions vars that need to be configured
    $acrName = "acr$($ProjectName -replace '-','')$Environment"
    Write-Host "  GitHub Actions Configuration Needed:" -ForegroundColor Yellow
    Write-Host "  ───────────────────────────────────────"
    Write-Host "  Repository Variables (Settings > Secrets and variables > Actions > Variables):"
    Write-Host "    ACR_NAME                 = $acrName"
    Write-Host "    ACR_LOGIN_SERVER         = $acrName.azurecr.us"
    Write-Host "    AZURE_RESOURCE_GROUP     = $ResourceGroup"
    Write-Host "    API_CONTAINER_APP_NAME   = $ProjectName-$Environment-api"
    Write-Host "    WEB_CONTAINER_APP_NAME   = $ProjectName-$Environment-web"
    Write-Host "    GATEWAY_CONTAINER_APP_NAME = $ProjectName-$Environment-mcp-gateway"
    Write-Host ""
}
