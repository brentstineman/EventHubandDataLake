using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendeeSimulator_Dev
{

    // represents one of the available session times
    public class ConfSession
    {
        public int RoomID; 
        public int MaxCapacity;
        public DateTime Start; 
        static Random rng = new Random();
        public List<SessionAttendence> Attendees = new List<SessionAttendence>();

        public VirtualProximitySensor ProximitySensor;

        // returns false if already full... 
        public bool TryAddAttendee(Attendee attendee)
        {
            if (Attendees.Count < MaxCapacity)
            {
                Attendees.Add(new SessionAttendence()
                {
                    BadgeID = attendee.AttendeeID,
                    Entered = (10*60) - TimeVariance(),
                    Left = (70*60) - TimeVariance()
                });

                return true; // was able to add attendee
            }
            else
                return false; // wasn't able to add
        }

        // adds an variance to arrival/departure times for attendees
        public static int TimeVariance()
        {
            short lateorearly = 1;
            if (rng.Next(100) > 50) // 50% chance of arriving early
                lateorearly = -1;

            // can be late or early by up to 10 minutes
            return (rng.Next(600) * lateorearly);

        }
    }

    // An attendee and the sessions they went too
    public class Attendee
    {
        public int AttendeeID;
    }

    // details of session attendence
    public class SessionAttendence
    {
        public int BadgeID;
        // amount of time late or early
        public int Entered;
        public int Left; 
    }
}
