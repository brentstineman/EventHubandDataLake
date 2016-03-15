using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace EventTypes
{
    [DataContract]
    public class ProximitySensorEvent
    {
        public ProximitySensorEvent()
        {
            this.ClearBadges();
        }

        public void ClearBadges()
        {
            FoundBadges = new AttendeeBadge[25];
        }

        [DataMember]
        public string SensorID { get; set; }

        [DataMember]
        public DateTime TransmissionTime { get; set; }

        [DataMember]
        public AttendeeBadge[] FoundBadges { get; set; }

        [DataContract]
        public class AttendeeBadge
        {
            [DataMember]
            public int BadgeID { get; set; }

            [DataMember]
            public int Strength { get; set; }
        }
    }

}
