using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GWManagementFunctions
{
    public class GWManagementOrchestrationCommand
    {
        [FunctionName("GWSendCommandWithResponse")]
        public static async Task<string> GWSendCommandWithResponse(
           [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            //Read input
            string input = context.GetInput<string>();
            if (input == "testCommand")
            {
                await context.CallActivityAsync<string>("SentDirectMessageToGW", $"{{\"msgId\":\"{context.InstanceId}\"}}");
            }
            else
            {
                //sending orchestration context id as msgId field.
                //msgId is being resent back by IQRF GW together with results for the invoked command.
                //This enables correlation of D2C messages floating to IoTHub with original commands sent as C2D messages
                var json = input.Replace("#MSG_ID#", context.InstanceId);
                await context.CallActivityAsync<string>("SentDirectMessageToGW", json);
            }
            
            using (var timeoutCts = new CancellationTokenSource())
            {
                // There is 30 second window for gateway to send response
                DateTime expiration = context.CurrentUtcDateTime.AddSeconds(30);
                Task timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);

                Task<string> gwIoTHuBResponseTask =
                    context.WaitForExternalEvent<string>("GWIoTHubResponse");

                Task winner = await Task.WhenAny(gwIoTHuBResponseTask, timeoutTask);
                if (winner == gwIoTHuBResponseTask)
                {
                    timeoutCts.Cancel();
                    //We received response from GW
                    return gwIoTHuBResponseTask.Result;
                }
            }
            return "Response did not arrive";
        }

        /// <summary>
        /// Send command thru IoT Hub direct message (C2D)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("SentDirectMessageToGW")]
        public static async Task<bool> SentDirectMessageToGW([ActivityTrigger] string message, ILogger log)
        {
            log.LogInformation("Sending Cloud-to-Device message");

            var serviceClient = ServiceClient.CreateFromConnectionString(Environment.GetEnvironmentVariable("IoTHubConnectionString"));

            //send message to device
            var commandMessage = new Message(Encoding.UTF8.GetBytes(message)) { Ack = DeliveryAcknowledgement.Full };
            await serviceClient.SendAsync(Environment.GetEnvironmentVariable("DeviceId"), commandMessage);

            //wait for ACK from device
            await ReceiveFeedbackAsync(serviceClient, log);

            return true;
        }  

        /// <summary>
        /// Receives the message ACK from the device (async).
        /// </summary>
        /// <param name="serviceClient">The service client.</param>
        /// <returns></returns>
        private static async Task ReceiveFeedbackAsync(ServiceClient serviceClient, ILogger log)
        {
            var feedbackReceiver = serviceClient.GetFeedbackReceiver();
            log.LogInformation("Waiting for C2D message receive ACK from service");

            var result = await feedbackReceiver.ReceiveAsync(TimeSpan.FromSeconds(1));

            //TODO implement logic for case when no ACK arrives
            if (result == null) return;

            log.LogInformation("Received ACK: {0}", string.Join(", ", result.Records.Select(f => f.StatusCode)));

            await feedbackReceiver.CompleteAsync(result);
        }
    }
}
