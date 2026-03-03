using '../main.bicep'

param environmentName = 'prod'
param location = 'usgovarizona'
param projectName = 'smart-ops-hub'
param imageTag = 'latest'
param sqlSkuName = 'S2'
param sqlSkuTier = 'Standard'
param logRetentionDays = 365
param gpt4oCapacity = 30
param gpt41Capacity = 30
