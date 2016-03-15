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
using Tweetinvi.Core.Credentials;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Auth.SetUserCredentials(TweetPublisher.Properties.Settings.Default.Consumer_Key, TweetPublisher.Properties.Settings.Default.Consumer_Secret,
                TweetPublisher.Properties.Settings.Default.Access_Token, TweetPublisher.Properties.Settings.Default.Access_Token_Secret);

            var filteredStream = Stream.CreateFilteredStream();

            filteredStream.AddTrack(TweetPublisher.Properties.Settings.Default.hashtag_filter);

            filteredStream.MatchingTweetReceived += (sender, tweetArgs) => {
                var tweet = tweetArgs.Tweet;

                Console.WriteLine(string.Format("{0} tweeted: {1}", tweet.CreatedBy.ScreenName, tweet.Text));
                Console.WriteLine(string.Empty);
                SendEvent(tweet);
            };
            filteredStream.StartStreamMatchingAllConditions(); // blocking call that will prevent app from exiting
        }

        public static void SendEvent(Tweetinvi.Core.Interfaces.ITweet tweet)
        {
            // Create EventHubClient object
            EventHubClient client = EventHubClient.CreateFromConnectionString(TweetPublisher.Properties.Settings.Default.eventHub_ConnectionString, 
                TweetPublisher.Properties.Settings.Default.eventHub_hubname);

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
