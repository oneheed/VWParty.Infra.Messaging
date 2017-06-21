using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;

namespace VWParty.Infra.Messaging.Core
{
    public class MessageClientBase<TInputMessage> : MessagePublisherBase<TInputMessage, DummyOutputMessage>
        where TInputMessage : InputMessageBase
    {
        internal protected MessageClientBase(string exchangeName, string exchangeType)
            : base(exchangeName, exchangeType)
        {
            this.IsWaitReturn = false;
        }

        internal protected MessageClientBase(string queueName)
            : base(queueName)
        {
            this.IsWaitReturn = false;
        }


        
        public virtual async Task PublishAsync(string routing, TInputMessage message)
        {
            await this.PublishMessageAsync(
                this.IsWaitReturn,
                MessageBusConfig.DefaultWaitReplyTimeOut,
                MessageBusConfig.DefaultMessageExpirationTimeout,
                routing, 
                message, 
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime,
                LogTrackerContext.Current);
        }


    }
}
