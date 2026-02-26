using '../main.bicep'

param environmentName = 'dev'
param location = 'usgovvirginia'
param projectName = 'smart-ops-hub'
param imageTag = 'latest'
param sqlSkuName = 'S1'
param sqlSkuTier = 'Standard'
param logRetentionDays = 30
param gpt4oCapacity = 10
