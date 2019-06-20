using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GWManagementFunctions
{
    public static class DeviceManagementOperations
    {

        /// <summary>
        /// Checks if device sleeps and if so waits for it to wake up
        /// </summary>
        /// <param name="context"></param>
        /// <param name="executionContext"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("DeviceSleepCheck")]
        public static async Task<ResponseObject> DeviceSleepCheck(
           [OrchestrationTrigger] DurableOrchestrationContext context, Microsoft.Azure.WebJobs.ExecutionContext executionContext, ILogger log)
        {
            var devAddr = context.GetInput<string>();
            var jsonTempaltesPath = Path.Combine(executionContext.FunctionDirectory, @"..\json_templates");
            var json = File.ReadAllText(jsonTempaltesPath + @"\enumerate.json");

            json = json.Replace("#DEV_ADDR#", $"{devAddr}");

            while (true)
            {
                var resEnum = await context.CallSubOrchestratorAsync<string>("GWSendCommandWithResponse", json);
     

                var errorMessage = Utils.CheckResponse(resEnum, out dynamic resEnumJObject);
                if (errorMessage != "" && resEnumJObject.data.statusStr != "info missing")
                {
                    log.LogInformation("Enumerate error: " + errorMessage);
                    return new ResponseObject(ResultMessages.ErrorResult, ResultMessages.DiscoveryError, errorMessage);
                }

                if (resEnumJObject.data.statusStr != "info missing")
                    return new ResponseObject(ResultMessages.Success, ResultMessages.EnumerateSuccess, resEnum);
                else
                {
                    // Orchestration sleeps until this time.
                    var nextCheck = context.CurrentUtcDateTime.AddSeconds(10);
                    await context.CreateTimer(nextCheck, CancellationToken.None);
                }
              
            }
        }
    }
}
