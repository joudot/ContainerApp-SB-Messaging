param imageTag string
param location string = resourceGroup().location
param minReplicas int = 0
param maxReplicas int = 65
param baseName string = 'juoudotsbsample'
param activeRevisionMode string = 'Single'
param containerPort int = 9000

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: '${baseName}-container-app-env'
}

resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' existing = {
  name: '${baseName}acr'
}

resource ai 'Microsoft.Insights/components@2020-02-02' existing = {
  name: '${baseName}-ApplicationInsights'
}

resource serviceBusQueueJob 'Microsoft.ServiceBus/namespaces/queues@2022-01-01-preview' existing = {
  name: '${baseName}sb-queue-job'
}

resource serviceBusQueueApp 'Microsoft.ServiceBus/namespaces/queues@2022-01-01-preview' existing = {
  name: '${baseName}sb-queue-app'
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' existing = {
  name: '${baseName}sb'
}
var serviceBusEndpoint = '${serviceBusNamespace.id}/AuthorizationRules/RootManageSharedAccessKey'

resource containerJobWorker 'Microsoft.App/jobs@2023-08-01-preview' = {
  name: 'servicebus-job-worker'
  location: location
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      secrets: [
        {
          name: 'sb-connection-string'
          value: listKeys(serviceBusEndpoint, serviceBusNamespace.apiVersion).primaryConnectionString
        }
        {
          name: 'container-registry-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
      triggerType: 'Event'
      replicaTimeout: 240
      replicaRetryLimit: 1
      eventTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
        scale: {
          minExecutions: minReplicas
          maxExecutions: maxReplicas
          pollingInterval: 10
          rules: [
            {
              auth: [
                {
                  secretRef: 'sb-connection-string'
                  triggerParameter: 'connection'
                }
              ]
              metadata: {
                queueName: serviceBusQueueJob.name
                namespace: serviceBusNamespace.name
                messageCount: '1'
              }
              name: 'queue-job'
              type: 'azure-servicebus'
            }
          ]
        }
      }
      registries: [
        {
          server: acr.properties.loginServer
          passwordSecretRef:'container-registry-password'
          username: acr.listCredentials().username
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${acr.properties.loginServer}/demo-sb-processor:${imageTag}'
          name: 'servicebus-job-worker'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Development'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: ai.properties.ConnectionString
            }
            {
              name: 'APPLICATIONINSIGHTS_INSTRUMENTATIONKEY'
              value: ai.properties.InstrumentationKey
            }
            {
              name: 'AZURE_SB_QUEUE_NAME_JOB'
              value: serviceBusQueueJob.name
            }
            {
              name: 'AZURE_SB_CONNECTION_STRING'
              secretRef: 'sb-connection-string'
            }
          ]
        }
      ]
    }
  }
}

resource containerAppWorker 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'servicebus-app-worker'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: activeRevisionMode
      secrets: [
        {
          name: 'container-registry-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'sb-connection-string'
          value: listKeys(serviceBusEndpoint, serviceBusNamespace.apiVersion).primaryConnectionString
        }
      ]
      registries: [
        {
          server: acr.properties.loginServer
          passwordSecretRef:'container-registry-password'
          username: acr.listCredentials().username
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${acr.properties.loginServer}/demo-sb-worker:${imageTag}'
          name: 'servicebus-app-worker'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Development'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: ai.properties.ConnectionString
            }
            {
              name: 'APPLICATIONINSIGHTS_INSTRUMENTATIONKEY'
              value: ai.properties.InstrumentationKey
            }
            {
              name: 'AZURE_SB_QUEUE_NAME_WORKER'
              value: serviceBusQueueApp.name
            }
            {
              name: 'AZURE_SB_CONNECTION_STRING'
              secretRef: 'sb-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: maxReplicas
        rules:  [
          {
            name: 'sb-size-rule'
            custom: {
              type: 'azure-servicebus'
              auth: [
                {
                  secretRef: 'sb-connection-string'
                  triggerParameter: 'connection'
                }
              ]
              metadata: {
                queueName: serviceBusQueueApp.name
                namespace: serviceBusNamespace.name
                messageCount: '5'
              }
            }
          }
        ]
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'servicebus-api-sender'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: activeRevisionMode
      ingress: {
        allowInsecure: true
        targetPort: containerPort
        external: true
      }
      secrets: [
        {
          name: 'container-registry-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'sb-connection-string'
          value: listKeys(serviceBusEndpoint, serviceBusNamespace.apiVersion).primaryConnectionString
        }
      ]
      registries: [
        {
          server: acr.properties.loginServer
          passwordSecretRef:'container-registry-password'
          username: acr.listCredentials().username
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${acr.properties.loginServer}/demo-sb-writer:${imageTag}'
          name: 'servicebus-api-sender'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Development'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: ai.properties.ConnectionString
            }
            {
              name: 'APPLICATIONINSIGHTS_INSTRUMENTATIONKEY'
              value: ai.properties.InstrumentationKey
            }
            {
              name: 'AZURE_SB_QUEUE_NAME_JOB'
              value: serviceBusQueueJob.name
            }
            {
              name: 'AZURE_SB_QUEUE_NAME_WORKER'
              value: serviceBusQueueApp.name
            }
            {
              name: 'AZURE_SB_CONNECTION_STRING'
              secretRef: 'sb-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules:  [
          {
            name: 'http-rule'
            http: {
              metadata:{
                concurrentRequests: '2'
              }
            }
          }
        ]
      }
    }
  }
}
