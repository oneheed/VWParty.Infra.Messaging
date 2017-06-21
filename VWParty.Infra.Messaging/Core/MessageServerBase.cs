using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;

namespace VWParty.Infra.Messaging.Core
{


    public abstract class MessageServerBase<TInputMessage> : MessageSubscriberBase<TInputMessage, DummyOutputMessage>
        where TInputMessage : InputMessageBase
    {
        protected MessageServerBase(string queueName)
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
                (TInputMessage message, LogTrackerContext logtracker) =>
                {
                    this.ExecuteSubscriberProcess(message, logtracker);
                    return null;
                },
                worker_count,
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime);
        }


        protected abstract void ExecuteSubscriberProcess(TInputMessage message, LogTrackerContext logtracker);

    }
}
