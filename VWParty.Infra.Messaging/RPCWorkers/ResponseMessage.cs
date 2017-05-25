using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.Messaging.Core;

namespace VWParty.Infra.Messaging.RPCWorkers
{
    [Serializable]
    public class ResponseMessage : OutputMessageBase
    {
        //public string exception;
        public string result_json;
        public string result_httpcontent;
    }
}
