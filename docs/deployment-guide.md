# Deployment Guide — Smart Ops Hub

## Prerequisites

### Azure
- An Azure Government subscription
- Permission to create resources (Contributor or Owner on the subscription)
- Azure CLI installed (`az --version` ≥ 2.60)

### GitHub
- Repository admin access (to configure secrets and environments)

---

## 1. One-Time Setup: OIDC Federated Credential

GitHub Actions authenticates to Azure via OIDC (no passwords stored). Create an Entra ID app registration with federated credentials.

```bash
# Login to Azure Government
az cloud set --name AzureUSGovernment
az login

# Create the app registration
az ad app create --display-name "smart-ops-hub-github-actions"

# Note the appId from the output, then create a service principal
az ad sp create --id <APP_ID>

# Assign Contributor on your subscription
az role assignment create \
  --assignee <APP_ID> \
  --role Contributor \
  --scope /subscriptions/<SUBSCRIPTION_ID>

# Create federated credential for main branch (push)
az ad app federated-credential create --id <APP_OBJECT_ID> --parameters '{
  "name": "github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<OWNER>/smart-ops-hub:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

# Create federated credential for pull requests
az ad app federated-credential create --id <APP_OBJECT_ID> --parameters '{
  "name": "github-pr",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<OWNER>/smart-ops-hub:pull_request",
  "audiences": ["api://AzureADTokenExchange"]
}'

# Create federated credential for dev environment
az ad app federated-credential create --id <APP_OBJECT_ID> --parameters '{
  "name": "github-env-dev",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<OWNER>/smart-ops-hub:environment:dev",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

---

## 2. GitHub Repository Configuration

### Secrets (Settings → Secrets and variables → Actions → Secrets)

| Secret | Value | Description |
|--------|-------|-------------|
| `AZURE_CLIENT_ID` | `<APP_ID>` | Entra ID app registration client ID |
| `AZURE_TENANT_ID` | `<TENANT_ID>` | Azure Government tenant ID |
| `AZURE_SUBSCRIPTION_ID` | `<SUB_ID>` | Target subscription ID |

### Variables (Settings → Secrets and variables → Actions → Variables)

| Variable | Value | Description |
|----------|-------|-------------|
| `ACR_NAME` | `acrsmartopshubdev` | Azure Container Registry name (no hyphens) |
| `ACR_LOGIN_SERVER` | `acrsmartopshubdev.azurecr.us` | ACR login server (`.azurecr.us` for Gov) |
| `AZURE_RESOURCE_GROUP` | `smart-ops-hub-dev-rg` | Resource group for container deployments |
| `API_CONTAINER_APP_NAME` | `smart-ops-hub-dev-api` | API Container App name |
| `WEB_CONTAINER_APP_NAME` | `smart-ops-hub-dev-web` | Web Container App name |
| `GATEWAY_CONTAINER_APP_NAME` | `smart-ops-hub-dev-mcp-gateway` | MCP Gateway Container App name |

### Environments (Settings → Environments)

Create a `dev` environment. Optionally add:
- Required reviewers for production
- Deployment branch rules (main only)

---

## 3. Deployment Flow

### First-time: Provision infrastructure

**Option A — Manual (local)**
```powershell
# Preview what will be created
./infra/scripts/deploy.ps1 -Environment dev -WhatIf

# Deploy
./infra/scripts/deploy.ps1 -Environment dev
```

**Option B — GitHub Actions (recommended)**

Go to **Actions → Infrastructure → Run workflow**:
- Environment: `dev`
- Action: `deploy`

### Ongoing: Application deployments

Once infrastructure exists, the CI/CD pipeline handles everything automatically:

```
Push to main → CI (build, test, lint, Docker) → CD (push images, deploy containers)
```

### Infrastructure changes

When you modify files in `infra/`:
- **PR**: Validates Bicep + posts what-if preview as PR comment
- **Merge to main**: Automatically deploys infrastructure changes

---

## 4. Pipeline Overview

| Workflow | Trigger | What it does |
|----------|---------|-------------|
| **CI** (`ci.yml`) | Push/PR to main | Build, test, lint, Docker build validation |
| **CD** (`cd.yml`) | CI success on main | Push images to ACR, deploy containers |
| **Infrastructure** (`infra.yml`) | Changes to `infra/`, manual dispatch | Validate/what-if/deploy Bicep |

### Dependency chain
```
infra.yml (provision resources)
    ↓ resources exist
ci.yml (build & test)
    ↓ success
cd.yml (push images & deploy)
```

---

## 5. Deploying to Production

1. Create a `prod` environment in GitHub with required reviewers
2. Add a federated credential for `environment:prod`
3. Run: **Actions → Infrastructure → Run workflow → prod / deploy**
4. Update the CD workflow to add a `deploy-prod` job with `environment: prod`

---

## 6. Verifying Deployment

After deployment, verify the endpoints:

```bash
# Health checks
curl https://<API_FQDN>/health
curl https://<API_FQDN>/health/ready
curl https://<WEB_FQDN>

# API
curl https://<API_FQDN>/api/agents
```

---

## 7. Troubleshooting

| Issue | Fix |
|-------|-----|
| OIDC auth fails | Verify federated credential `subject` matches exactly (repo name, branch/env) |
| ACR push fails | Ensure managed identity has `AcrPush` on the registry |
| Container App won't start | Check Container Apps logs: `az containerapp logs show --name <app> --resource-group <rg>` |
| SQL connection fails | Managed identity needs db_owner on the database |
| Bicep what-if fails | Resource group must exist — run deploy script with `-WhatIf` or create it manually |
