using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;
using VWParty.Infra.Messaging.Core;
using VWParty.Infra.Messaging.RPCWorkers;

namespace Zeus.Messaging.Worker
{

    public class POCWorker : WorkerQueue
    {
        public POCWorker() : base("rpc_test")
        {

        }

        public int count = 0;

        protected override ResponseMessage ExecuteSubscriberProcess(RequestMessage message, LogTrackerContext logtracker)
        {
            Interlocked.Increment(ref count);
            return new ResponseMessage()
            {
                result_json = message.input_json
            };
        }
    }
}
