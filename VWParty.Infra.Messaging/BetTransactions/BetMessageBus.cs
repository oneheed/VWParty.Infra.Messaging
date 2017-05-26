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
    public class BetMessagePublisher : MessagePublisherBase<BetTransactionMessage, OutputMessageBase>
    {
        public BetMessagePublisher() : base("tp-transaction", "direct")
        {

        }

        
    }


    public class BetMessageSubscriber : MessageSubscriberBase<BetTransactionMessage, OutputMessageBase>
    {
        public BetMessageSubscriber(string queueName) : base(queueName)
        {

        }


    }





    [Obsolete("已廢除，請改用 BetMessagePublisher / BetMessageSubscriber 替代", true)]
    public class BetMessageBus
    {
        //private ConnectionFactory connectionFactory = null;
        //private IConnection connection = null;

        protected string exchangeTopic { get; private set; }
        //{
        //    get
        //    {
        //        return "tp-transaction";
        //    }
        //}
        
        public BetMessageBus() //: this("10.101.6.173", 5672)
            : this("tp-transaction")
        {

        }

        public BetMessageBus(string exchangeName)
        {
            this.exchangeTopic = exchangeName;
        }






        public void PublishMessage(string brandId, BetTransactionMessage message)
        {
            using (var connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection()) //this.connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(
                    exchangeTopic,
                    "direct",
                    true,
                    false,
                    null);

                try
                {
                    var corrId = Guid.NewGuid().ToString("N");
                    IBasicProperties props = channel.CreateBasicProperties();
                    props.ContentType = "application/json";

                    channel.BasicPublish(
                        exchange: exchangeTopic,
                        routingKey: brandId,
                        basicProperties: props,
                        body: Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message)));

                    return;
                }
                finally
                {
                    //channel.QueueDelete(replyQueue.QueueName);
                }
            }
        }


        public delegate object process_subscribed_message(BetTransactionMessage betmsg);

        private void SubscribeMessage(string queue, bool response, process_subscribed_message processor)
        {
            if (processor == null) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(queue)) throw new ArgumentNullException();

            //int concurrent = 0;
            ManualResetEvent wait = new ManualResetEvent(false);

            using (var connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection()) //this.connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue, true, false, false);
                var consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(
                    queue: queue,
                    noAck: false,   // you must MANUAL send ack back when you complete message process.
                    consumer: consumer);

                bool running = false;
                EventHandler<BasicDeliverEventArgs> worker = (model, ea) =>
                {
                    try
                    {
                        running = true;
                        wait.Reset();
                        if (this._stop == true) return;

                        var body = ea.Body;
                        var message = Encoding.Unicode.GetString(body);

                        if (string.IsNullOrEmpty(message))
                        {
                            // got empty message
                            return;
                        }

                        BetTransactionMessage btm = JsonConvert.DeserializeObject<BetTransactionMessage>(message);
                        if (btm == null)
                        {
                            // message deserialize fail
                            return;
                        }

                        object respMsg = processor(btm);

                        if (response)
                        {
                            // return
                            channel.BasicPublish(
                                "",
                                ea.BasicProperties.ReplyTo,
                                ea.BasicProperties,
                                Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(respMsg)));
                        }
                        //Task.Delay(1000).Wait();
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                    finally
                    {
                        if (this._stop == true) wait.Set();
                        running = false;
                    }
                };

                consumer.Received += worker;
                //while(wait.WaitOne(100)==false && this._stop==false && running == false);
                while(true)
                {
                    if (wait.WaitOne(100) == true) break;
                    if (running) continue;
                    if (this._stop == true) break;
                }
                consumer.Received -= worker;
            }
        }



        private Task[] running_worker_tasks = null;
        private bool _stop = true;


        public void Stop()
        {
            this._stop = true;
            //this._wait.WaitOne();
            Task.WaitAll(this.running_worker_tasks);
        }

        public void StartSubscribeWorker(string queue, bool response, process_subscribed_message processor, int worker_count)
        {
            this._stop = false;

            Task[] tasks = new Task[worker_count];

            for (int index = 0; index < worker_count; index++)
            {
                tasks[index] = Task.Run(() => {
                    this.SubscribeMessage(queue, response, processor);
                });
            }

            this.running_worker_tasks = tasks;

        }
    }
}
