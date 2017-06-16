using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging.Core
{
    public abstract class MessageClientBase<TInputMessage> : MessagePublisherBase<TInputMessage, DummyOutputMessage>
        where TInputMessage : InputMessageBase
    {
        protected MessageClientBase(string exchangeName, string exchangeType)
            : base(exchangeName, exchangeType)
        {
            this.IsWaitReturn = false;
        }

        protected MessageClientBase(string queueName)
            : base(queueName)
        {
            this.IsWaitReturn = false;
        }
        
        public async Task PublishAsync(string routing, TInputMessage message)
        {
            await this.PublishMessageAsync(routing, message);
        }
    }
}
