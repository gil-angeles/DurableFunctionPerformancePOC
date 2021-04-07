using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PerformanceTest
{
    public static class PerformanceServiceBus
    {

        [FunctionName(nameof(RunOrchestratorServiceBus))]
        public static async Task RunOrchestratorServiceBus(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var parallelTasks = new List<Task>();

            var activityList = await context.CallActivityAsync<List<int>>(nameof(MainActivityServiceBus), "Test");

            for (int i = 0; i < activityList.Count; i++)
            {
                Task task = context.CallActivityAsync(nameof(ServiceBusQueueActivity), activityList[i]);
                parallelTasks.Add(task);
            }

            await Task.WhenAll(parallelTasks);
        }

        [FunctionName(nameof(SendMessageServiceBus))]
        public static async Task SendMessageServiceBus(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            await using (ServiceBusClient client = new ServiceBusClient(Environment.GetEnvironmentVariable("ConnectionStringServiceBus")))
            {
                ServiceBusSender sender = client.CreateSender(Environment.GetEnvironmentVariable("QueueNameServiceBus"));

                // create a message that we can send
                ServiceBusMessage message = new ServiceBusMessage("Launch Orchestrator now!");

                // send the message
                await sender.SendMessageAsync(message);
                log.LogInformation($"Sent a single message to the queue: {Environment.GetEnvironmentVariable("QueueNameServiceBus")}");
            }
        }

        [FunctionName(nameof(ServiceBusQueueTrigger))]
        public static async Task ServiceBusQueueTrigger(
            [ServiceBusTrigger("queue-durablefunction-performance-poc", Connection = "AzureWebJobsServiceBus")]
            string myQueueItem,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
                
            string instanceId = await starter.StartNewAsync(nameof(RunOrchestratorServiceBus), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            log.LogInformation($"ServiceBus queue trigger function processed message: {myQueueItem}");
        }

        [FunctionName(nameof(MainActivityServiceBus))]
        public static List<int> MainActivityServiceBus([ActivityTrigger] string item, ILogger log)
        {
            List<int> tasks = new List<int>();

            int length = 1000;

            for (int i = 0; i < length; i++)
            {
                tasks.Add(i);
            }

            return tasks;

        }

        [FunctionName(nameof(ServiceBusQueueActivity))]
        public static void ServiceBusQueueActivity([ActivityTrigger] int interval, ILogger log)
        {
            log.LogInformation($"Service bus activity: {interval}");
        }


    }
}