using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EventTypes
{
    [DataContract]
    public class VendorCheckin
    {
        [DataMember]
        public string VendorID { get; set; }
        [DataMember]
        public string BadgeID { get; set; }
    }

}
