using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace EventTypes
{

    [DataContract]
    public class AttendeeAdministrationEvent
    {
        public enum ActionType
        {
            Add,
            Change
        }

        [DataMember]
        public long AttendeeID { get; set; }
        [DataMember]
        public ActionType Action { get; set; }
        [DataMember]
        public DateTime CreatedAt { get; set; }
        [DataMember]
        public Hashtable Values { get; set; }
    }
}
