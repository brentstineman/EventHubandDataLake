
using Microsoft.ServiceBus;
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

        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(EventProcessor.Properties.Settings.Default.eventHubConnectionString);
            EventHubDescription ehd = namespaceManager.GetEventHub(EventProcessor.Properties.Settings.Default.eventHubPath);
            namespaceManager.CreateConsumerGroupIfNotExistsAsync(ehd.Path, EventProcessor.Properties.Settings.Default.consumerGroupName);

            myProcessorHost = new EventProcessorHost(Guid.NewGuid().ToString(), EventProcessor.Properties.Settings.Default.eventHubPath, 
                EventProcessor.Properties.Settings.Default.consumerGroupName, EventProcessor.Properties.Settings.Default.eventHubConnectionString, 
                EventProcessor.Properties.Settings.Default.storageConnectionString);

            myProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>().Wait();

            RunAsync(_cancellationTokenSource.Token).Wait();

            _cancellationTokenSource.Cancel();
            _runCompleteEvent.WaitOne();

            // Unregister the processor 
            myProcessorHost.UnregisterEventProcessorAsync().Wait();
        }

        private static async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

    }
}
