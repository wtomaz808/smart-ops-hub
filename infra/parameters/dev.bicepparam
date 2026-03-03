using '../main.bicep'

param environmentName = 'dev'
param location = 'usgovarizona'
param projectName = 'smart-ops-hub'
param imageTag = 'latest'
param sqlSkuName = 'S1'
param sqlSkuTier = 'Standard'
param logRetentionDays = 30
param gpt4oCapacity = 10
param gpt41Capacity = 10
