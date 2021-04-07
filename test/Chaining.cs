using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace test
{
    public static class Chaining
    {
        [FunctionName("Chaining")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                var x = await context.CallActivityAsync<string>("F1", null);
                var y = await context.CallActivityAsync<string>("F2", x);
                var z = await context.CallActivityAsync<string>("F3", y);
                return await context.CallActivityAsync<string>("F4", z);
            }
            catch (Exception)
            {
                // Error handling or compensation goes here.
                return "oh no";
            }
        }

        [FunctionName("F1")]
        public static string Function1([ActivityTrigger] string name, ILogger log)
        {
            string result = "Result 1";
            log.LogInformation($"Primera function {result}.");
            return result;
        }

        [FunctionName("F2")]
        public static string Function2([ActivityTrigger] string name, ILogger log)
        {
            string result = name + " Result 2";
            log.LogInformation($"Segunda function {result}.");
            return result;
        }
        [FunctionName("F3")]
        public static string Function3([ActivityTrigger] string name, ILogger log)
        {
            string result = name + " Result 3";
            log.LogInformation($"Primera function {result}.");
            return result;
        }
        [FunctionName("F4")]
        public static string Function4([ActivityTrigger] string name, ILogger log)
        {
            string result = name + " Result 4";
            log.LogInformation($"Primera function {result}.");
            return result;
        }

        [FunctionName("Chaining_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Chaining", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}