using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;

namespace VWParty.Infra.Messaging.Core
{
    public abstract class RpcServerBase<TInputMessage, TOutputMessage> : MessageSubscriberBase<TInputMessage, TOutputMessage>
        where TInputMessage : InputMessageBase
        where TOutputMessage : OutputMessageBase
    {
        protected RpcServerBase(string queueName)
            : base(queueName)
        {
            
        }



        public virtual async Task StartWorkersAsync()
        {
            await this.StartWorkersAsync(1);
        }

        public virtual async Task StartWorkersAsync(int worker_count)
        {
            await this.StartWorkersAsync(
                this.ExecuteSubscriberProcess,
                worker_count,
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime);
        }

        protected virtual TOutputMessage ExecuteSubscriberProcess(TInputMessage message, LogTrackerContext logtracker)
        {
            return null;
        }

    }
}
