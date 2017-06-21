using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;

namespace VWParty.Infra.Messaging.Core
{
    public abstract class RpcClientBase<TInputMessage, TOutputMessage> : MessagePublisherBase<TInputMessage, TOutputMessage>
        where TInputMessage : InputMessageBase
        where TOutputMessage : OutputMessageBase
    {
        protected RpcClientBase(string exchangeName, string exchangeType) 
            : base(exchangeName, exchangeType)
        {
            this.IsWaitReturn = true;
        }

        protected RpcClientBase(string queueName)
            : base(queueName)
        {
            this.IsWaitReturn = true;
        }


        public virtual TOutputMessage Call(string routing, TInputMessage message)
        {
            //return this.PublishMessageAsync(routing, message, LogTrackerContext.Current).Result;
            return this.PublishMessageAsync(
                this.IsWaitReturn,
                MessageBusConfig.DefaultWaitReplyTimeOut,
                MessageBusConfig.DefaultMessageExpirationTimeout,
                routing,
                message,
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime,
                LogTrackerContext.Current).Result;

        }

        public virtual async Task<TOutputMessage> CallAsync(string routing, TInputMessage message)
        {
            //return await this.PublishMessageAsync(routing, message, LogTrackerContext.Current);
            return await this.PublishMessageAsync(
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
