using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Messaging
{
    [Serializable]
    public class ResponseMessage
    {
        public string exception;
        public string result_json;
        public string result_httpcontent;
    }
}
