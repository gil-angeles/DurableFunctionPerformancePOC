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
    public static class Performance
    {
        class ItemsSent
        {
            [JsonProperty(PropertyName = "item")]
            public string Item { get; set; }
        }

        [FunctionName(nameof(RunOrchestrator))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var item = context.GetInput<string>();

            await context.CallActivityAsync<string>(nameof(MainActivity), item);
        }

        [FunctionName(nameof(EventGridTriggerStart))]
        public static async Task EventGridTriggerStart(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var eventGridData = JsonConvert.DeserializeObject<ItemsSent>(eventGridEvent.Data.ToString());

            string instanceId = await starter.StartNewAsync(nameof(RunOrchestrator), null, eventGridData.Item);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        }

        [FunctionName(nameof(PublishEventTopic))]
        public static async Task<IActionResult> PublishEventTopic(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var topicKey = Environment.GetEnvironmentVariable("topicKey");
            var topicEndPoint = Environment.GetEnvironmentVariable("topicEndPoint");
            var topicHostName = new Uri(topicEndPoint).Host;

            TopicCredentials topicCredentials = new TopicCredentials(topicKey);
            EventGridClient client = new EventGridClient(topicCredentials);
            var events = GetEventsList();
            await client.PublishEventsAsync(topicHostName, events);
            log.LogInformation("Published Events Completed");
            return new OkResult();
        }

        static IList<EventGridEvent> GetEventsList()
        {
            List<EventGridEvent> eventsList = new List<EventGridEvent>();
            for (int i = 0; i < 1000; i++)
            {
                eventsList.Add(new EventGridEvent()
                {
                    Id = Guid.NewGuid().ToString(),
                    //Topic = "dt-learn-mcx",
                    EventType = "MCX",
                    Data = new ItemsSent
                    {
                        Item =  i.ToString()
                    },
                    EventTime = DateTime.Now,
                    Subject = "New Item for MCX",
                    DataVersion = "2.0",
                });
                
            }
            
            return eventsList;
        }

        [FunctionName(nameof(MainActivity))]
        public static string MainActivity([ActivityTrigger] string item, ILogger log)
        {
            log.LogInformation(item);
            return item;
        }


    }
}