using EventTypes;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Credentials;
using System.Configuration;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            
            Auth.SetUserCredentials("Access_Token", "Access_Token_Secret", "Consumer_Key", "Consumer_Secret");

            var filteredStream = Stream.CreateFilteredStream();
            filteredStream.AddTrack("hashtagfilter");

            filteredStream.MatchingTweetReceived += (sender, tweetArgs) => { 
                Console.WriteLine(string.Format("{0} tweeted: {1}", tweetArgs.Tweet.CreatedBy.ScreenName, tweetArgs.Tweet.Text));
                Console.WriteLine(string.Empty);
                SendEvent(tweetArgs.Tweet);
            };
            filteredStream.StartStreamMatchingAllConditions();
        }

        public static void SendEvent(Tweetinvi.Core.Interfaces.ITweet tweet)
        {
            // Create EventHubClient object
            EventHubClient client = EventHubClient.Create(ConfigurationManager.AppSettings["eventHubName"]);

            //Random random = new Random();
            TweetEvent myEvent = new TweetEvent() { 
                Author = tweet.CreatedBy.ScreenName,
                Text = tweet.Text, 
                CreatedAt = tweet.CreatedAt
             };
            var serializedString = JsonConvert.SerializeObject(myEvent);
            EventData data = new EventData(Encoding.Unicode.GetBytes(serializedString))
            {
                PartitionKey = "WebSource"
            };

            // Set user properties if needed
            data.Properties.Add("Type", "TweetEvent");

            // Send the metric to Event Hub
            client.Send(data);
        }

    }
}
