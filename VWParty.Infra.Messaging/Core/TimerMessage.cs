using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging.Core
{
    public class TimerMessage : InputMessageBase
    {
        public string ID { get; set; }
        public DateTime RunAt { get; set; }


        public string RouteKey { get; set; }
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
        public string ExchangeType { get; set; }
        public string MessageType { get; set; }
        public string MessageText { get; set; }
    }
}
