using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace InfiniteScaleSharedState
{
    public class Sample
    {
        private const string MyStateFileName = "state.txt";
        private readonly string MyStateFilePath = Path.Combine(/*Path.GetTempPath()*/"D:\\home\\data\\", nameof(Sample));
        private readonly string Instance = $"{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")} - {Environment.GetEnvironmentVariable("INSTANCE_META_PATH")}";
        private readonly LogRecord logRecord;
        private static Dictionary<string, string> MyState = new Dictionary<string, string>();


        public Sample()
        {
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            this.logRecord = new LogRecord(connectionString);
        }

        [FunctionName(nameof(Write))]
        public async Task<IActionResult> Write(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"Invoking the {nameof(Write)} function on {Instance}.");

            if (!Directory.Exists(MyStateFilePath))
            {
                Directory.CreateDirectory(MyStateFilePath);
            }

            var formattedGuid = CreateStateFile();            

            log.LogInformation($"Invoked the {nameof(Write)} function on {Instance}. Stored `{formattedGuid}`");
            
            return new OkObjectResult($"Stored `{formattedGuid}`");
        }

        [FunctionName(nameof(Read))]
        public async Task<IActionResult> Read(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var guid = Guid.NewGuid();
            log.LogInformation($"Invoking the {nameof(Read)} function on {Instance}.");

            // First try to work with an `IMemoryCache` or static dictionary. 
            // If this object doesn't contain information, read the state file.
            string stateFile = Path.Combine(MyStateFilePath, MyStateFileName);
            if (File.Exists(stateFile))
            {
                log.LogDebug("Found the state path directory.");
                
                var content = System.IO.File.ReadAllText(stateFile);
                log.LogDebug("Read content of state file.");

                return new OkObjectResult($"{nameof(Read)} on {Instance} found `{content}`");
            }

            log.LogWarning($"No state file path found on {Instance}.");

            return new NotFoundResult();
            
        }

        [FunctionName(nameof(Invalidate))]
        public async Task Invalidate(
            [ServiceBusTrigger("commands", "%SubscriptionName%", Connection = "ServiceBusConnection")] 
            string messageId, // This is always an integer in my poc, so the casting below will succeed.
            ILogger log)
        {
            log.LogInformation($"{nameof(Invalidate)} is triggered with message: {messageId}");
            string stateFile = Path.Combine(MyStateFilePath, MyStateFileName);
            string formattedGuid;
            if (File.Exists(stateFile))
            {
                log.LogInformation($"Found `{stateFile}` on {Instance}");
                formattedGuid = System.IO.File.ReadAllText(stateFile);
                log.LogInformation($"Found `{formattedGuid}` on {Instance}");
            }
            else
            {
                log.LogInformation($"Creating`{stateFile}` on {Instance}");
                formattedGuid = CreateStateFile();
                log.LogInformation($"Created `{formattedGuid}` on {Instance}");
            }


            await this.logRecord.Store(new LogRecord.Entity(int.Parse(messageId), Instance, formattedGuid));

        }

        private string CreateStateFile()
        {
            if (!Directory.Exists(MyStateFilePath))
            {
                Directory.CreateDirectory(MyStateFilePath);
            }
            var guid = Guid.NewGuid();
            string stateFile = Path.Combine(MyStateFilePath, MyStateFileName);
            var formattedGuid = guid.ToString("D");
            System.IO.File.WriteAllText(stateFile, formattedGuid);
            return formattedGuid;
        }
    }
}