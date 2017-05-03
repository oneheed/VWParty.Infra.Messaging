using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.POC
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: RabbitMQ.POC.exe {mode} {exchange} {queuename}");
                Console.WriteLine("  - mode: (available value) pub | sub");
                return;
            }

            switch (args[0].ToLower())
            {
                case "pub":
                    StartPublisher(args[1]);
                    break;

                case "sub":
                    StartSubscriber(args[1], args[2]);
                    break;

                default:
                    break;
            }
        }

        private static Random rnd = new Random();


        private static void StartSubscriber(string topic, string queue)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "ANDREW-UBUNTU"
            };

            //int concurrent = 0;
            ManualResetEvent wait = new ManualResetEvent(false);
            
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(topic, "fanout", true);
                channel.QueueDeclare(queue, true, false, false);

                //var queueName = channel.QueueDeclare().QueueName;
                //channel.QueueBind(
                //    queue: queue,
                //    exchange: topic,
                //    routingKey: "");

                Console.WriteLine(" [*] Waiting for logs");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    wait.Reset();

                    //Interlocked.Increment(ref concurrent);
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.Write(" [{1}] {0}", message, Thread.CurrentThread.ManagedThreadId);
                    Task.Delay(800).Wait();

                    // return
                    channel.BasicPublish("", ea.BasicProperties.ReplyTo, ea.BasicProperties, Encoding.UTF8.GetBytes($"DONE: {queue}"));


                    channel.BasicAck(ea.DeliveryTag, false);
                    Console.WriteLine("");
                    //Interlocked.Decrement(ref concurrent);

                    wait.Set();
                };

                

                //var consumer = new QueueingBasicConsumer(channel);
                //while(true)
                //{
                //    var ea = consumer.Queue.Dequeue();
                //    var body = ea.Body;
                //    var message = Encoding.UTF8.GetString(body);
                //    Console.WriteLine(" [x] {0}", message);
                //}




                channel.BasicConsume(
                    queue: queue,
                    noAck: false,   // you must MANUAL send ack back when you complete message process.
                    consumer: consumer);

                Console.WriteLine("Press [ENTER] to quit.. ({0})", Thread.CurrentThread.ManagedThreadId);
                Console.ReadLine();
                
                wait.WaitOne();
                Console.WriteLine("done");
                //SpinWait.SpinUntil(() => { return concurrent == 0; });
                //Console.WriteLine(concurrent);
            }
        }

        private static void StartPublisher(string topic)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "ANDREW-UBUNTU"
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(topic, "fanout", true);



                while (true)
                {
                    // prepare response queue
                    var replyQueue = channel.QueueDeclare();
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(replyQueue.QueueName, true, consumer);



                    var message = string.Format("Random NO: #{0:000}", rnd.Next(1000));
                    var corrId = Guid.NewGuid().ToString("N");

                    IBasicProperties props = channel.CreateBasicProperties();
                    props.ContentType = "text/plain";
                    //props.DeliveryMode = 2;
                    props.Expiration = "10000"; // per message expiration

                    // setup return mechanism
                    props.ReplyTo = replyQueue.QueueName;
                    props.CorrelationId = corrId;

                    channel.BasicPublish(
                        exchange: topic,
                        routingKey: "",
                        basicProperties: props,
                        body: Encoding.UTF8.GetBytes(message));

                    Console.WriteLine(" [x] Sent: {0}", message);


                    BasicDeliverEventArgs ea;
                    if (consumer.Queue.Dequeue(3000, out ea) == true)
                    {
                        // assure ea.BasicProperties.CorrelationId == corrId;
                        Console.WriteLine("return: {0}", Encoding.UTF8.GetString(ea.Body));
                    }
                    else
                    {
                        Console.WriteLine("RPC timeout.");
                    }
                    channel.QueueDelete(replyQueue.QueueName);
                    

                    Task.Delay(1000).Wait();
                }
            }
        }
    }
}
