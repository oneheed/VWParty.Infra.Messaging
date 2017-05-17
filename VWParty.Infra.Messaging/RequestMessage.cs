using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Messaging
{
    [Serializable]
    public class RequestMessage
    {
        public string request_id { get; set; }
        public string function { get; set; }
        public string vender { get; set; }
        public string method { get; set; }
        public string input_json { get; set; }

        [Obsolete("", true)]
        public int retryCount { get; set; }
        [Obsolete("", true)]
        public string CorrelationId { get; set; }

        public RequestMessage()
        {
            //this.retryCount = 0;
        }
    }
}
