using EventTypes;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;

namespace AttendeeSimulator_Dev
{
    public class VirtualProximitySensor
    {
        // id for this sensor device
        public string SensorID;
        // how frequently we're sending messages
        public int SendRate;

        static Random rng = new Random();

        public ManualResetEvent WaitHandle; 
        
        public VirtualProximitySensor(string SensorID)
        {
            this.SensorID = SensorID;

        }

        // to be put into a thread pool
        // paramter: rate of time compression to use
        public void RunSession(List<SessionAttendence> Attendees, DateTime SessionStart, int BatchSize=25, int TimeCompression=1)
        {
            int badgeCnt = 0; 
            int sendinterval = 5; // in seconds 
            DateTime virtualClock = SessionStart;
            ProximitySensorEvent sensorEvent = new ProximitySensorEvent()
            {
                SensorID = this.SensorID
            };

            int totalruns = (10 + 60 + 10) * 60 / sendinterval; // takes # of minutes, converts to 5 second blocks
            
            for (int i = 0; i<=totalruns; i++)
            {
                int timelapse = i * sendinterval; 

                sensorEvent.TransmissionTime = virtualClock;
                sensorEvent.ClearBadges();

                // send events for attendees... 
                foreach (SessionAttendence attendee in Attendees)
                {
                    if ((timelapse >= attendee.Entered) && (timelapse <= attendee.Left)) 
                    {
                        badgeCnt++;
                        sensorEvent.FoundBadges[badgeCnt-1] = new ProximitySensorEvent.AttendeeBadge()
                            {
                                BadgeID = attendee.BadgeID,
                                Strength = rng.Next(100)
                            };

                        if (badgeCnt >= 25)
                        {
                            Console.WriteLine("Sensor '{0}' is reporting {1} attendees at {2:H:mm:ss}", this.SensorID, badgeCnt, virtualClock);
                            SendProximityEvent(sensorEvent);
                            badgeCnt = 0;
                        }
                    }
                }

                // blast any remaining badge results to event hub
                if (badgeCnt > 0)
                {
                    Console.WriteLine("Sensor '{0}' is reporting {1} attendees at {2:H:mm:ss}", this.SensorID, badgeCnt, virtualClock);
                    SendProximityEvent(sensorEvent);
                    badgeCnt = 0;
                }

                Thread.Sleep((sendinterval* 1000)/TimeCompression); // sleep 5 seconds... 
                virtualClock = virtualClock.AddSeconds(sendinterval);
            }

            this.WaitHandle.Set();
        }

        private void SendProximityEvent(ProximitySensorEvent sensorEvent)
        {
            // Create EventHubClient
            EventHubClient client = EventHubClient.Create(ConfigurationManager.AppSettings["eventHubName"]);

            var serializedString = JsonConvert.SerializeObject(sensorEvent);
            EventData data = new EventData(Encoding.Unicode.GetBytes(serializedString))
            {
                PartitionKey = sensorEvent.SensorID
            };

            // Set user properties if needed
            data.Properties.Add("Type", "ProximitrySensor");

            // Send the metric to Event Hub
            client.SendAsync(data);

            // clear sent badges
            sensorEvent.ClearBadges();
        }
    }
}
