using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;

namespace InfiniteScaleSharedState
{
    public class LogRecord
    {
        private readonly string connectionString;
        private const string Insert = @"INSERT INTO [dbo].[Logging]
           ([Id]
           ,[Instance]
           ,[MessageId]
           ,[FormattedGuid]
           ,[Created])
     VALUES
           (@Id
           ,@Instance
           ,@MessageId
           ,@FormattedGuid
           ,@Created)";

        public LogRecord(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task Store(LogRecord.Entity record)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(Insert, new { 
                Id = record.Id,
                Instance = record.Instance, 
                MessageId = record.MessageId,
                FormattedGuid = record.FormattedGuid,
                Created = record.Created });
        }

        public class Entity
        {
            public Guid Id = Guid.NewGuid();
            public DateTime Created { get; } = DateTime.UtcNow;

            public int MessageId { get; }
            public string Instance { get; }
            public string FormattedGuid { get; }

            public Entity(int messageId, string hostname, string formattedGuid)
            {
                MessageId = messageId;
                Instance = hostname;
                FormattedGuid = formattedGuid;
            }
        }
    }
}
/*
 Initialize script for the database:

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
 */