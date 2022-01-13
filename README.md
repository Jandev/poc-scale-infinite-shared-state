# Project name

What is this about

# Set up

This is what you need to do to run it yourself

# Sample usage

Needs a `local.settings.json` file with the following format:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "SqlConnectionString": "Server=tcp:[databasename].database.windows.net,1433;Initial Catalog=global-scale;Persist Security Info=False;User ID=[username];Password=[password];MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
    "ServiceBusConnection": "Endpoint=sb://jv-poc-global-scale.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=[key]]",
    "SubscriptionName": "weu" // or "cus"
  }
}
```
