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

namespace VWParty.Infra.Messaging.BetTransactions
{
    public class BetMessageBus
    {
        //private ConnectionFactory connectionFactory = null;
        //private IConnection connection = null;

        const string exchangeTopic = "tp-transaction";
        
        public BetMessageBus() //: this("10.101.6.173", 5672)
        {

        }






        public void PublishMessage(string brandId, BetTransactionMessage message)
        {
            using (var connection = QueueConfig.DefaultConnectionFactory.CreateConnection()) //this.connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                //string exchangeName = "tp-transaction";
                //string brandId = "letou";

                channel.ExchangeDeclare(
                    exchangeTopic,
                    "direct",
                    true,
                    false,
                    null);

                //var replyQueue = channel.QueueDeclare();

                try
                {
                    // prepare response queue
                    //var consumer = new QueueingBasicConsumer(channel);
                    //channel.BasicConsume(replyQueue.QueueName, true, consumer);



                    //var message = "-------------------------------------";
                    var corrId = Guid.NewGuid().ToString("N");

                    IBasicProperties props = channel.CreateBasicProperties();
                    props.ContentType = "application/json";
                    //props.DeliveryMode = 2;
                    //props.Expiration = "10000"; // per message expiration

                    // setup return mechanism
                    //props.ReplyTo = replyQueue.QueueName;
                    //props.CorrelationId = corrId;

                    channel.BasicPublish(
                        exchange: exchangeTopic,
                        routingKey: brandId,
                        basicProperties: props,
                        body: Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message)));

                    //Console.WriteLine();
                    //Console.WriteLine("Sent: {0}", message);
                    return;

                    //BasicDeliverEventArgs ea;
                    //if (consumer.Queue.Dequeue(15000, out ea) == true)
                    //{
                    //    // assure ea.BasicProperties.CorrelationId == corrId;
                    //    // return;
                    //}
                    //else
                    //{
                    //    //Console.WriteLine("* RPC timeout.");
                    //    //break;
                    //    throw new TimeoutException();
                    //}
                }
                finally
                {
                    //channel.QueueDelete(replyQueue.QueueName);
                }
            }
        }


        public delegate object process_subscribed_message(BetTransactionMessage betmsg);

        public void SubscribeMessage(string queue, bool response, process_subscribed_message processor)
        {
            if (processor == null) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(queue)) throw new ArgumentNullException();

            //int concurrent = 0;
            ManualResetEvent wait = new ManualResetEvent(false);

            using (var connection = QueueConfig.DefaultConnectionFactory.CreateConnection()) //this.connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                //channel.ExchangeDeclare(exchangeTopic, "fanout", true);
                channel.QueueDeclare(queue, true, false, false);

                //var queueName = channel.QueueDeclare().QueueName;
                //channel.QueueBind(
                //    queue: queue,
                //    exchange: topic,
                //    routingKey: "");

                var consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(
                    queue: queue,
                    noAck: false,   // you must MANUAL send ack back when you complete message process.
                    consumer: consumer);

                EventHandler<BasicDeliverEventArgs> worker = (model, ea) =>
                {
                    wait.Reset();

                    try
                    {
                        //Interlocked.Increment(ref concurrent);
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

                        channel.BasicAck(ea.DeliveryTag, false);
                        Console.WriteLine("");
                        //Interlocked.Decrement(ref concurrent);
                    }
                    finally
                    {
                        wait.Set();
                    }

                };
                consumer.Received += worker;


                //var consumer = new QueueingBasicConsumer(channel);
                //while(true)
                //{
                //    var ea = consumer.Queue.Dequeue();
                //    var body = ea.Body;
                //    var message = Encoding.Unicode.GetString(body);
                //    Console.WriteLine(" [x] {0}", message);
                //}






                Console.WriteLine("Press [ENTER] to quit.. ({0})", Thread.CurrentThread.ManagedThreadId);
                Console.ReadLine();

                wait.WaitOne();
                consumer.Received -= worker;
                //Console.WriteLine("done");
                //SpinWait.SpinUntil(() => { return concurrent == 0; });
                //Console.WriteLine(concurrent);
            }
        }

    }
}
