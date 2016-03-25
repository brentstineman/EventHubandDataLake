#Event Hub and Data Lake Sample
**This solution is based on a preview version of the Azure Data Lake nuget package. Expect that some changes may be necessary when the first production version of the package is released.**

This sample solution was created to go my “Introduction to the Azure Service Bus Event Hub” presentation first given at That Conference 2014, in Wisconsin. The solution centers on a simulated conference tracking system, Contoso Conferencing. And it has since been updated to also allow for demonstration of writing Event Hub messages directly to Azure Data Lake. 

This code and the presentation center on using the Event Hub to ingest event messages from various sources (passive proximity sensors, a web site, mobile devices, etc…) and allowing them to be consumed for near real time modelling and/or persistence. However, the modelling and data mining aspects of the solution were not covered as part of this sample/presentation. 

The solution is divided into 7 projects that I’ve separated into three categories. 

###Event Processors:
**Event Processor** – a sample app showing how to consume messages from the event hub via .NET and the “SimpleEventProcessor” which automatically manages receivers and their connections to partitions. This was used as a sample by the other two presentations to demonstrate getting the events. 

###Event Publishers:
**AttendeeSimulator** – This solution will simulate passive proximity sensors placed in various rooms at the conference. It shouldn’t be confused with an AI that simulates user traffic. The simulator is just a simple example to simulate users entering and leaving various session rooms. 

**Tweet Publisher** – This console app monitors twitter for a given hashtag (say the one for the conference you’re presenting at), then publishes those to the event hub.

**VendorCheckInSample** – a web project that can be published to Azure Web Sites that demonstrates using javascript to publish to the Event Hub and simulates a mobile app used by vendors to scan attendee badges. Care should be taken to stress to attendees that since a SAS is present in the javascript, this should NEVER be done in a production solution.

###Helper Projects:
**Event Types** – A class library that contains the class defintions of the various messages being sent to and received from the hub. Created so application can easily deserialize the data. 

**Signature Generator** – a simple console app to show customers how to create SAS as well as to help generate the SAS tokens for use in the various applications. 

###Using the Demo Projects
The following are the various setup steps needed for each of the running applications. These assume you’ve created an Event Hub and created a policy with “Send” permission and one with full permissions (send, listen, manage).

**Event Processor** – open the app.config file and provide the requested values for the settings in the AppSettings section. Consumer Group name is arbritary but you may want to make sure it’s separate from other consumers. The Service Bus connection string here needs to be for a policy that has send, listen, manage permissions. If you do not with to persiste data to the Azure Data Lake, simply leave the values prefixed with aad or adl at their current '<*>' values.

**AttendeeSimulator** – You’ll need a SQL Database (I used Azure SQL DB) that has a copy of the Adventureworks 2014 sample database as well as an Azure Storage Account (to be used by the Simple Receiver). After you set up the Azure SQL DB, be sure to modify the firewall rules so you can connect to it locally and from anywhere you plan to deploy the sample applications too.

Update the app.config file, use a Service Bus connection string policy that only has send permission. You’ll also need to include the connection string for the SQL database and the name of your event hub. You can make further adjustments to the simulator by playing with the values for the VirtualClock start time (Program.cs, line 35), as well as the values of RoomCount, slotCnt, TimeCompression, and attendeeCnt. Be careful about setting the time compression to high with too many users. It can cause the simulator to “gag” a bit. 

**TweetPublisher** – update the app.config file with the Service Bus connection string (that has send permissions) and event hub you want to send to. Also update it with the twitter developer tokens and the hashtag filter you want to monitor. 

**VendorCheckInSample** – The code in here is all in the default.aspx page, which means it’s in clear text) and available to your session attendees. You’ll need to update the SAS in line 41, the Event Hub URL in line 46, and the SQL Database connection strings on lines 60 and 61. 

**Event Types** – no work needed

**Signature Generator** – nothing needed here.  
