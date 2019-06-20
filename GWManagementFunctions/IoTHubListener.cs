using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

namespace GWManagementFunctions
{
    public static class IoTHubListener
    {
        /// <summary>
        /// Receives device message from IoT Hub and continues respective durable function orchestration context
        /// </summary>
        /// <param name="message">message from IQRG GW arriving from IoT Hub</param>
        /// <param name="log"></param>
        /// <param name="client">Orchestration client</param>
        /// <returns></returns>
        [FunctionName("IoTHubListener")]
        public static async Task Run([IoTHubTrigger("messages/events", Connection = "IoTHubEventHubEndpointConnectionString")]EventData message,
            ILogger log,
            [OrchestrationClient] DurableOrchestrationClient client)
        {

            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");

            var s = Encoding.UTF8.GetString(message.Body.Array);
            dynamic jsonObject = JsonConvert.DeserializeObject(s);

            //Raising event - waking up respective orchestration context 
            await client.RaiseEventAsync($"{jsonObject.msgId}", "GWIoTHubResponse", s);
        }
    }
}