using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SimulatedIQRFGW
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Build config, pull out connection strings
            IConfiguration config = new ConfigurationBuilder()
                  .AddJsonFile("AppSettings.json", true, true)
                  .Build();

            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(config["deviceConnectionString"], TransportType.Amqp);
            while(true)
            {
                //Receive commands in infinite loop
                await ReceiveCommands(deviceClient);
            }
        }

        private static async Task ReceiveCommands(DeviceClient deviceClient)
        {
            Console.WriteLine("\nDevice waiting for commands from IoTHub...\n");
            Console.WriteLine("Use the IoT Hub Azure Portal to send a message to this device.\n");

            Message receivedMessage;
            string messageData;

            //Wait for message with default timeout
            receivedMessage = await deviceClient.ReceiveAsync();

            if (receivedMessage != null)
            {
                messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);

                try
                {
                    dynamic jsonObject = JsonConvert.DeserializeObject(messageData);

                    //Resend orchestrationId as part of response being sent thru IoTHub (D2C)
                    //In real scenario this message will contain response to command
                    Message eventMessage = new Message(Encoding.UTF8.GetBytes($"{{\"msgId\":\"{jsonObject.msgId}\"}}"));
                    await deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
                }
                catch(Exception e) {
                    Console.WriteLine(e.Message);
                }

                //Read message properties example 
                //int propCount = 0;
                //foreach (var prop in receivedMessage.Properties)
                //{
                //    Console.WriteLine("\t\tProperty[{0}> Key={1} : Value={2}", propCount++, prop.Key, prop.Value);
                //}

                //Mark message as completed/processed
                await deviceClient.CompleteAsync(receivedMessage);
            }
            else
            {
                Console.WriteLine("\t{0}> Timed out", DateTime.Now.ToLocalTime());
            }
        }
    }
}
