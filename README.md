# Project name

I found an [answer on Stack Overflow stating the local storage is shared between Function App instances](https://stackoverflow.com/a/39400126/352640).  
This struck me as odd, so I wanted to try it out.

# Set up

I've manually spun up an environment with the following services:

- Function App in Central US
- Function App in West Europe
- SQL Server + database
- Service Bus Namespace + Topic

The database is used for storing data which I can query & analyze quickly.  
This is what you need to initialize the database:

## Database

```sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Logging](
	[Id] [uniqueidentifier] NOT NULL,
	[Instance] [nvarchar](255) NOT NULL,
	[MessageId] [int] NOT NULL,
    [FormattedGuid] [nvarchar](100) NOT NULL,
	[Created] [datetime] NOT NULL
) ON [PRIMARY]
GO
```

## Function App settings

For local development, the Azure Functions needs a `local.settings.json` file with the following information:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "SqlConnectionString": "Server=tcp:[databasename].database.windows.net,1433;Initial Catalog=global-scale;Persist Security Info=False;User ID=[username];Password=[password];MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
    "ServiceBusConnection": "Endpoint=sb://[ServiceBusNamespace].windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=[key]",
    "SubscriptionName": "weu" // or "cus"
  }
}
```

It goes without saying, you also need to add the settings to the actual deployed Azure Functions.

# Running it

To make sure we don't have any concurrency issues writing the state file initially, it's a good idea to invoke the `https://my-function-app.azurewebsites.net/api/Write` endpoint (on all deployed Function Apps) before populating the Service Bus Topic.

The `Producer` application also needs a connection to the correct Service Bus Namespace & Topic.
The Service Bus Topic has a subscription called `weu` and `cus`, where all messages are routed to.  
The `Producer` will send messages to the Topic which can be picked up by both Azure Functions.

You should be able to see records being added to the database now. If not, there's probably something missing in the configuration or firewall rules of the database.

# Results

I've tried the scenario with 10.000 messages being sent to the Topic and being picked up by the Function Apps.  
The expectation, based on the Stack Overflow answer, would be I'd only see 2 guids stored in the database, as they share their local storage between instances of a Function App.

The actual results show otherwise:  
![Results in Elastic Premium](/docs/images/premium-mode.png)

![Results in Serverless](/docs/images/serverless-mode.png)

As I'd expected before reading the answer, each instance has it's own local storage.

The above is true when I was using the `Temp`-directory in the Azure Functions.  
As mentioned in Chris' comment over here: https://github.com/Azure/azure-functions-host/issues/3626#issuecomment-431485622 this should only be used for local Function invocation.  
Sharing date has to be done in the `D:\home\data\`-folder.

When storing the state file over there, the results are like Chris is describing in his SO answer:  
![Results in Serverless using home folder](/docs/images/serverless-home-folder.png)
