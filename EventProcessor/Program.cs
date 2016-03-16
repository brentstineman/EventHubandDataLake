using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventProcessor
{
    class Program
    {
        private static EventProcessorHost myProcessorHost;

        static void Main(string[] args)
        {
            myProcessorHost = new EventProcessorHost(EventProcessor.Properties.Settings.Default.eventHubPath, EventProcessor.Properties.Settings.Default.consumerGroupName,
                EventProcessor.Properties.Settings.Default.eventHubConnectionString, EventProcessor.Properties.Settings.Default.storageConnectionString);

            myProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>().Wait();

            // never ending loop
            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
