using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GWManagementFunctions
{
    public static class GWManagementOrchestrationAction
    {

        [FunctionName("GWManagementAction")]
        public static async Task<string> RunGWManagementAction(
            [OrchestrationTrigger] DurableOrchestrationContext context, Microsoft.Azure.WebJobs.ExecutionContext executionContext, ILogger log)
        {
            string result = "";
            string reqBody = context.GetInput<string>();
            dynamic reqBodyJsonObject = JsonConvert.DeserializeObject(reqBody);

            var jsonTempaltesPath = System.IO.Path.Combine(executionContext.FunctionDirectory, @"..\json_templates");
            string json;

            switch ($"{ reqBodyJsonObject.requestType}")
            {
                case "unbondDevice":

                    //1. Wait for device to wake up
                    //Setting custom status
                    context.SetCustomStatus("{\"Status\":\"Device is Sleeping\"}");
                    await context.CallSubOrchestratorAsync<ResponseObject>("DeviceSleepCheck", reqBodyJsonObject.devAddr);
                    context.SetCustomStatus("{\"Status\":\"Device has woken up\"}");

                    //2. Remove bonded sensor from GW
                    context.SetCustomStatus($"{{\"Status\":\"Removing device from GW\"}}");
                    json = System.IO.File.ReadAllText(jsonTempaltesPath + @"\remove_bonded_device.json");
                    json = json.Replace("#DEV_ADDR#", $"{reqBodyJsonObject.devAddr}");

                    result = await context.CallSubOrchestratorAsync<string>("GWSendCommandWithResponse", json);

                    //3. Update DB
                    context.SetCustomStatus($"{{\"Status\":\"Updating DB entry\"}}");
                    string unbondQuery = "UPDATE dbo.Sensor " +
                       $"SET [GatewayIndex] = NULL, [GatewayID] = NULL WHERE ID = {reqBodyJsonObject.sensorID}";
                    await context.CallActivityAsync<string>("InvokeDatabaseOperation", unbondQuery);

                    break;

                case "testCommand":
                    result = await context.CallSubOrchestratorAsync<string>("GWSendCommandWithResponse", "testCommand");
                    break;

                default:
                    break;
            }

            return result;
        }


        /// <summary>
        /// Initiates management command loop, in real implementation req.body should contain command
        /// </summary>
        /// <param name="req"></param>
        /// <param name="starter"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("OrchestrationFunction_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
                    [HttpTrigger(AuthorizationLevel.System, "post")]HttpRequestMessage req,
                    [OrchestrationClient]DurableOrchestrationClient starter,
                    ILogger log)
        {

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("GWManagementAction", await req.Content.ReadAsStringAsync());
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}