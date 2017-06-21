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

        public TimerClient(string queueName) : base("scheduler")
        {
            this._parent_queueName = queueName;
        }

        public TimerClient(string exchangeName, string exchangeType) : base("scheduler")
        {
            this._parent_exchangeName = exchangeName;
            this._parent_exchangeType = exchangeType;
        }

        public async Task PublishAsync(string routing, TInputMessage message, TimeSpan delay)
        {
            await this.PublishMessageAsync(
                routing, 
                new TimerMessage()
                {
                    ID = Guid.NewGuid().ToString(),
                    RunAt = DateTime.Now.Add(delay).ToUniversalTime(),

                    RouteKey = routing,
                    ExchangeName = this._parent_exchangeName,
                    ExchangeType = this._parent_exchangeType,
                    QueueName = this._parent_queueName,
                    MessageText = JsonConvert.SerializeObject(message)
                },
                LogTrackerContext.Current);
        }

    }
}
