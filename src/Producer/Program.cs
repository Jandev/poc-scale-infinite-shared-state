using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Producer
{
    class Program
    {
        static async Task Main()
        {
            var host = CreateDefaultBuilder().Build();

            // Invoke Worker
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;
            var workerInstance = provider.GetRequiredService<Worker>();
            await workerInstance.DoWork();

            host.Run();

            Console.WriteLine("Press any key to end the application");
            Console.ReadKey();
        }

        static IHostBuilder CreateDefaultBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(app =>
                {
                    app.AddJsonFile("appsettings.json");
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<Worker>();
                });
        }
    }

    /// <summary>
    /// Implementation from the Service Bus Messaging documentation site: https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?WT.mc_id=AZ-MVP-5003246
    /// </summary>
    internal class Worker
    {
        private readonly ILogger<Worker> logger;

        // connection string to your Service Bus namespace
        private readonly string connectionString;

        // name of your Service Bus queue
        private readonly string topicName;

        // number of messages to be sent to the queue
        private readonly int numOfMessages;

        // the client that owns the connection and can be used to create senders and receivers
        static ServiceBusClient client;

        // the sender used to publish messages to the queue
        static ServiceBusSender sender;

        private static int i;


        public Worker(
            IConfiguration configuration,
            ILogger<Worker> logger)
        {
            this.logger = logger;
            connectionString = configuration["ServiceBus:ConnectionString"];
            topicName = configuration["ServiceBus:TopicName"];
            numOfMessages = int.Parse(configuration["ServiceBus:NumberOfMessagesToAdd"]);
        }

        public async Task DoWork()
        {
            // The Service Bus client types are safe to cache and use as a singleton for the lifetime
            // of the application, which is best practice when messages are being published or read
            // regularly.
            //
            // Create the clients that we'll use for sending and processing messages.
            client = new ServiceBusClient(connectionString);
            sender = client.CreateSender(topicName);

            try
            {

                i = 1;
                // create a batch 
                while (i < numOfMessages)
                {
                    using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
                    {
                        for (; i <= numOfMessages; i++)
                        {
                            logger.LogInformation("Adding message {Identifier}", i);

                            // try adding a message to the batch
                            if (!messageBatch.TryAddMessage(new ServiceBusMessage($"{i}")))
                            {
                                logger.LogWarning("Batch too large, sending.");
                            }

                            if (i % 1000 == 0)
                            {
                                await SendCreatedBatch(messageBatch);
                                i++;
                                break;
                            }

                        }
                    }
                }
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }

        private async Task SendCreatedBatch(ServiceBusMessageBatch messageBatch)
        {
            try
            {
                // Use the producer client to send the batch of messages to the Service Bus queue
                await sender.SendMessagesAsync(messageBatch);
                logger.LogInformation($"A batch of {messageBatch.Count} messages has been published to the queue.");
            }
            catch (ServiceBusException sbe)
            {
                logger.LogError(sbe, sbe.Message);
            }
        }
    }
}
