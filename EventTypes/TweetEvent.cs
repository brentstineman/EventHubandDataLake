using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace EventTypes
{
    [DataContract]
    public class TweetEvent
    {
        [DataMember]
        public string Author { get; set; }
        [DataMember]
        public string Text { get; set; }
        [DataMember]
        public DateTime CreatedAt { get; set; }
    }
}
