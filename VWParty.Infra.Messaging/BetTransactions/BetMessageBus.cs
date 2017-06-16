using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VWParty.Infra.Messaging;
using VWParty.Infra.Messaging.Core;

namespace VWParty.Infra.Messaging.BetTransactions
{
    [Obsolete("", true)]
    public class BetMessagePublisher : MessagePublisherBase<BetTransactionMessage, OutputMessageBase>
    {
        public BetMessagePublisher() : base("tp-transaction", "direct")
        {

        }

        
    }


    [Obsolete("", true)]
    public class BetMessageSubscriber : MessageSubscriberBase<BetTransactionMessage, OutputMessageBase>
    {
        public BetMessageSubscriber(string queueName) : base(queueName)
        {

        }


    }

}
