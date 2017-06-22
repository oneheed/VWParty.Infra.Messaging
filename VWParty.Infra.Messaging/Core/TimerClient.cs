using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;

namespace VWParty.Infra.Messaging.Core
{
    public class TimerClient<TInputMessage> : MessageClientBase<TimerMessage>
        where TInputMessage : InputMessageBase
    {
        private string _parent_queueName = null;
        private string _parent_exchangeName = null;
        private string _parent_exchangeType = null;


        protected TimerClient(string schedulerQueueName, string forwardQueueName, string forwardExchangeName, string forwardExchangeType) : base(schedulerQueueName)
        {
            this._parent_queueName = forwardQueueName;
            this._parent_exchangeName = forwardExchangeName;
            this._parent_exchangeType = forwardExchangeType;
        }

        public TimerClient(string queueName) : this("scheduler", queueName, null, null) { }

        public TimerClient(string exchangeName, string exchangeType) : this("scheduler", null, exchangeName, exchangeType) { }

        public async Task PublishAsync(string routing, TInputMessage message, TimeSpan delay)
        {
            await this.PublishMessageAsync(
                this.IsWaitReturn,
                MessageBusConfig.DefaultWaitReplyTimeOut,
                MessageBusConfig.DefaultMessageExpirationTimeout,
                routing, 
                new TimerMessage()
                {
                    ID = Guid.NewGuid().ToString(),
                    RunAt = DateTime.Now.Add(delay).ToUniversalTime(),

                    RouteKey = routing,
                    ExchangeName = this._parent_exchangeName,
                    ExchangeType = this._parent_exchangeType,
                    QueueName = this._parent_queueName,

                    MessageType = typeof(TInputMessage).FullName,
                    MessageText = JsonConvert.SerializeObject(message)
                },
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime,
                LogTrackerContext.Current);
        }
    }
}
