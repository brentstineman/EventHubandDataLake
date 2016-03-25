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
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.Management.DataLake.Store.Models;

namespace EventProcessor
{
    public class SimpleEventProcessor : IEventProcessor
    {
        IDictionary<string, int> map;
        PartitionContext partitionContext;
        Stopwatch checkpointStopWatch;

        TokenCredentials myADTokenCredentials;
        DataLakeStoreFileSystemManagementClient myadlFileSystemClient;
        string dlFileName;

        bool fsDataLakeEnabled = false;

        public SimpleEventProcessor()
        {
            this.map = new Dictionary<string, int>();

            // set feature switch for Azure Data Lake output
            // disable if the folder setting starts with "<" (aka is at the default)
            fsDataLakeEnabled = !EventProcessor.Properties.Settings.Default.adl_destFolder.StartsWith("<");
        }

        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine(string.Format("SimpleEventProcessor initialize.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset));

            if (fsDataLakeEnabled) // if we're doing data lake output
            {
                // set up our target file name
                dlFileName = string.Format("/{0}/{1}", EventProcessor.Properties.Settings.Default.adl_destFolder, EventProcessor.Properties.Settings.Default.adl_fileName);

                // get security token for Azure Data Lake
                Task<TokenCredentials> t = AuthenticateApplication(EventProcessor.Properties.Settings.Default.aad_tenantId,
                    EventProcessor.Properties.Settings.Default.aad_resource, EventProcessor.Properties.Settings.Default.aad_appClientId,
                    EventProcessor.Properties.Settings.Default.aad_clientSecret);
                t.Wait();
                myADTokenCredentials = t.Result;

                //create Azure Data Lake Client using the token
                myadlFileSystemClient = new DataLakeStoreFileSystemManagementClient(myADTokenCredentials);
                myadlFileSystemClient.SubscriptionId = EventProcessor.Properties.Settings.Default.adl_subscriptionID;

                // create the directory in the Data Lake Store
                myadlFileSystemClient.FileSystem.Mkdirs(EventProcessor.Properties.Settings.Default.adl_destFolder, EventProcessor.Properties.Settings.Default.adl_accountName);

                // create a file in the Data Lake Store (if it already exists, an exception is thrown, ignore that one)
                try
                {
                    myadlFileSystemClient.FileSystem.Create(dlFileName, EventProcessor.Properties.Settings.Default.adl_accountName, new MemoryStream(), true);
                }
                catch (Microsoft.Rest.Azure.CloudException exp)
                {
                    // if file already exists, ignore and continue. Otherwise re-throw exception
                    if (!exp.Response.Content.Contains("FileAlreadyExistsException"))
                        throw exp; // rethrow exception
                }
            }

            this.partitionContext = context;
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> events)
        {
            try
            {
                var memoryStream = new MemoryStream();
                var streamWriter = new StreamWriter(memoryStream);

                foreach (EventData eventData in events)
                {
                    // capture the event's data to the stream so we can write it to the Data Lake Store
                    streamWriter.WriteLine(Encoding.Unicode.GetString(eventData.GetBytes()));

                    // output the event to the console                    
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

                if (fsDataLakeEnabled) // if we're doing data lake output
                {
                    // save the events to Data Lake
                    streamWriter.Flush();
                    if (memoryStream.Length > 0)
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        myadlFileSystemClient.FileSystem.ConcurrentAppend(dlFileName, memoryStream, EventProcessor.Properties.Settings.Default.adl_accountName, AppendModeType.Autocreate);
                    }
                }

                memoryStream.Dispose();
                streamWriter.Dispose();

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

            if (fsDataLakeEnabled) // if we're doing data lake output
            {
                // dispose of our Data Lake Store client
                myadlFileSystemClient.Dispose();
            }

            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        #region Data Lake Methods
        async public static Task<TokenCredentials> AuthenticateApplication(string tenantId, string resource, string appClientId, string clientSecret)
        {
            var authenticationContext = new AuthenticationContext("https://login.windows.net/" + tenantId);
            var credential = new ClientCredential(clientId: appClientId, clientSecret: clientSecret);
            var tokenAuthResult = await authenticationContext.AcquireTokenAsync(resource: "https://management.core.windows.net/", clientCredential: credential);

            return new TokenCredentials(tokenAuthResult.AccessToken);
        }

        #endregion
    }
}
