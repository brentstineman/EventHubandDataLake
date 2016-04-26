using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Threading;
using System.Globalization;
using System.Diagnostics;
using System.Configuration;

namespace AttendeeSimulator_Dev
{
    class Program
    {
        // lists for generated values
        static List<List<ConfSession>> SlotList = new List<List<ConfSession>>();
        static List<Attendee> AttendeeList = new List<Attendee>();

        static Random rng = new Random();
        static int RoomCount = 0;
        static int RoomCapacity = 25;
        static int TimeCompression = 1;

        private static List<ManualResetEvent> _waitHandles = new List<ManualResetEvent>();

        public static readonly string[] AvailableSensors = { "AAA001", "BBB002", "CCC003", "DDD004", "EEE005", "FFF006", "GGG007", "HHH008", "III009", "JJJ010", "ZZZ011", "LLL012", "MMM013",  "NNN014", "OOO015" };

        static void Main(string[] args)
        {
            // set a starting time for the conference
            DateTime virtualClock;
            DateTime.TryParseExact("2014/08/10:0750", "yyyy/MM/dd:HHmm", null,
                       DateTimeStyles.None, out virtualClock);

            //********************
            // these variables help control generation
            //********************
            RoomCount = 4; // # of rooms
            int slotCnt = 6; // # of session slots per day
            TimeCompression = 100; // multiplier to elapsed time, 100 means real time
            int attendeeCnt = 100; // number of attendees

            if (RoomCount > AvailableSensors.Length)
                throw new Exception("Room count exceeds available sensors"); 

            #region GenerateSessions/Slots
            //********************
            // generate session (slots*rooms)
            //********************
            for (int slot = 1; slot <= slotCnt; slot++)
            {
                List<ConfSession> tmpRoomList = new List<ConfSession>();
                for (int room = 1; room <= RoomCount; room++)
                {
                    ConfSession tmpRoom = new ConfSession()
                    {
                        RoomID = room,
                        MaxCapacity = RoomCapacity,
                        ProximitySensor = new VirtualProximitySensor(AvailableSensors[room-1])
                    };
                    tmpRoomList.Add(tmpRoom); // add room to list
                }
                SlotList.Add(tmpRoomList); // add room list to session slot
            }
            #endregion

            #region Generate Attendees
            //********************
            // generate attendee list
            // I used a copy of the Contoso2013 database to get attendee names
            //********************
            SqlConnection sdwDBConnection = new SqlConnection(AttendeeSimulator_Dev.Properties.Settings.Default.SQLConnectionString);
            sdwDBConnection.Open();
            SqlCommand queryCommand = new SqlCommand("select top("+attendeeCnt+") FirstName,LastName,BusinessEntityID FROM Person.Person ORDER BY BusinessEntityID", sdwDBConnection);
            SqlDataReader reader = queryCommand.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Attendee tmpAttendee = new Attendee()
                    {
                        AttendeeID = reader.GetInt32(2)
                    };
                    AttendeeList.Add(tmpAttendee);
                }
            }
            sdwDBConnection.Close();
            #endregion

            #region schedule attendees
            //********************
            // for each available slot, distribute attendees in sessions
            //********************
            for (int i = 0; i < slotCnt; i++)
            {
                foreach(Attendee attendee in AttendeeList)
                {
                    PickARoom(i, attendee);
                }
            }
            #endregion

            //********************
            // comment out this section if you just want to generate a list of attendees...
            // run the sensors on a loop that 'blasts' found Ids every 5 seconds
            //********************
            #region Run Simulation
            foreach (List<ConfSession> SessionSlot in SlotList)
            {
                Console.WriteLine("**                    **");
                Console.WriteLine("** Kicking off first session at: {0:H:mm} **",virtualClock);
                Console.WriteLine("**                    **");

                _waitHandles.Clear();

                foreach(ConfSession session in SessionSlot)
                {
                    session.Start = virtualClock; 
                    // set up the proximity sensor to run... 
                    session.ProximitySensor.WaitHandle = new ManualResetEvent(false);
                    _waitHandles.Add(session.ProximitySensor.WaitHandle);

                    // add to thread pool
                    ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(SensorThread), session);
                }
                // start threads

                // wait for them to finish
                System.Threading.WaitHandle.WaitAll(_waitHandles.ToArray<ManualResetEvent>());

                // update virtual clock
                virtualClock = virtualClock.AddMinutes(80);

                Console.WriteLine("**                    **");
                Console.WriteLine("** Session Slot Ended at: {0:H:mm}**", virtualClock);
                Console.WriteLine("**                    **");
                // assume 10 minutes between sessions (assuming time compression = 100, so no overlap
                Thread.Sleep(600000 / TimeCompression); // sleep until next session
                virtualClock = virtualClock.AddMinutes(10);
            }
            #endregion

        }

        public static void SensorThread(object sender)
        {
            if (sender == null) return;

            ConfSession session = sender as ConfSession;
            session.ProximitySensor.RunSession(session.Attendees, session.Start, 25, TimeCompression);
        }

        // this method will determine which room (if any) an attendee ended up in
        private static void PickARoom(int SessionSlot, Attendee attendee)
        {
            int room = -1; // default, no room/session picked
            int tryCount = 0;
            bool gotin = false; 

            // 95% likely the attendee goes to the session
            if (rng.Next(100) <= 95)
            {
                // do until we've tried max times or we find a room
                do
                {
                    room = rng.Next(RoomCount); // pick a room
                    gotin = SlotList[SessionSlot][room].TryAddAttendee(attendee);
                    tryCount++; 
                }
                while ((tryCount < RoomCount) && (!gotin));
            }
        }

    }
}
