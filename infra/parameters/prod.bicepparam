using '../main.bicep'

param environmentName = 'prod'
param location = 'usgovarizona'
param imageTag = 'latest'
param sqlSkuName = 'S2'
param sqlSkuTier = 'Standard'
param logRetentionDays = 365
param gpt4oCapacity = 30
param gpt41Capacity = 30
