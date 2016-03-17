using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using EventTypes;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace EventProcessor
{
    public class SimpleEventProcessor : IEventProcessor
    {
        IDictionary<string, int> map;
        PartitionContext partitionContext;
        Stopwatch checkpointStopWatch;

        TokenCredentials myADTokenCredentials;

        public SimpleEventProcessor()
        {
            this.map = new Dictionary<string, int>();
        }

        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine(string.Format("SimpleEventProcessor initialize.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset));

            // calling ansyc method 
            Task<TokenCredentials> t = AuthenticateApplication(EventProcessor.Properties.Settings.Default.aad_tenantId,
                EventProcessor.Properties.Settings.Default.aad_resource, EventProcessor.Properties.Settings.Default.aad_appClientId,
                EventProcessor.Properties.Settings.Default.aad_clientSecret);
            t.Wait();
            myADTokenCredentials = t.Result;

            this.partitionContext = context;
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> events)
        {
            try
            {
                foreach (EventData eventData in events)
                {
                    if (eventData.Properties["Type"].ToString().Equals("TweetEvent"))
                    {
                        TweetEvent tweet = JsonConvert.DeserializeObject<TweetEvent>(Encoding.Unicode.GetString(eventData.GetBytes()));

                        Console.WriteLine(string.Format("**Tweet**, Created By: '{1}': '{2}'\n\r",
                            this.partitionContext.Lease.PartitionId, tweet.Author, tweet.Text));
                    }
                    else if (eventData.Properties["Type"].ToString().Equals("VendorCheckin"))
                    {
                        VendorCheckin checkin = JsonConvert.DeserializeObject<VendorCheckin>(Encoding.UTF8.GetString(eventData.GetBytes()));

                        Console.WriteLine(string.Format("**VendorCheckin**, Created By: '{1}': '{2}'",
                            this.partitionContext.Lease.PartitionId, checkin.VendorID, checkin.BadgeID));
                        Console.WriteLine(" ");
                    }
                    else if (eventData.Properties["Type"].ToString().Equals("ProximitrySensor"))
                    {
                        string tmp = Encoding.Unicode.GetString(eventData.GetBytes());
                        ProximitySensorEvent proximityevent = JsonConvert.DeserializeObject<ProximitySensorEvent>(tmp);

                        Console.WriteLine(string.Format("**ProximitrySensor**, Created By: '{1}': '{2:HH:mm}'",
                            this.partitionContext.Lease.PartitionId, proximityevent.SensorID, proximityevent.TransmissionTime));
                    }
                }

                //Call checkpoint every n minutes, so that worker can resume processing from that point if restarted
                if (this.checkpointStopWatch.Elapsed.Ticks > TimeSpan.FromMinutes(1).Ticks)
                {
                    await context.CheckpointAsync();
                    lock (this)
                    {
                        this.checkpointStopWatch.Reset();
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("Error in processing: " + exp.Message);
            }
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine(string.Format("Processor Shuting Down.  Partition '{0}', Reason: '{1}'.", this.partitionContext.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        #region Data Lake Methods
        async public static Task<TokenCredentials> AuthenticateApplication(string tenantId, string resource, string appClientId, string clientSecret)
        {
            var authContext = new AuthenticationContext("https://login.microsoftonline.com/" + tenantId);
            var credential = new ClientCredential(appClientId, clientSecret);

            var tokenAuthResult = await authContext.AcquireTokenAsync(resource, credential);

            return new TokenCredentials(tokenAuthResult.AccessToken);
        }

        #endregion
    }
}
