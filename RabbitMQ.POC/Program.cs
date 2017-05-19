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

            Console.WriteLine("Mode:         {0}", args[0]);
            Console.WriteLine("Exchange:     {0}", args[1]);
            Console.WriteLine("Queue Name:   {0}", args[2]);

            switch (args[0].ToLower())
            {
                case "pub":
                    StartPublisher(args[1]);
                    break;

                case "sub":
                    StartSubscriber(args[1], args[2], false);
                    break;

                case "rpc":
                    StartSubscriber(args[1], args[2], true);
                    break;

                default:
                    break;
            }
        }

        private static Random rnd = new Random();

        private static ConnectionFactory factory = new ConnectionFactory()
        {
            HostName = "127.0.0.1",
            Port = 5672
        };

        private static void StartSubscriber(string topic, string queue, bool response)
        {


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
                EventHandler<BasicDeliverEventArgs> worker = (model, ea) =>
                {
                    wait.Reset();

                    //Interlocked.Increment(ref concurrent);
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.Write(" [{1}] process ({0}) ...", message, Thread.CurrentThread.ManagedThreadId);
                    Task.Delay(500 + rnd.Next(100)).Wait();
                    Console.Write("done!");

                    if (response)
                    {
                        // return
                        channel.BasicPublish(
                            "", 
                            ea.BasicProperties.ReplyTo, 
                            ea.BasicProperties, 
                            Encoding.UTF8.GetBytes(String.Format("DONE: {0}", queue)));
                    }

                    channel.BasicAck(ea.DeliveryTag, false);
                    Console.WriteLine("");
                    //Interlocked.Decrement(ref concurrent);

                    wait.Set();
                };
                consumer.Received += worker;


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
                consumer.Received -= worker;
                Console.WriteLine("done");
                //SpinWait.SpinUntil(() => { return concurrent == 0; });
                //Console.WriteLine(concurrent);
            }
        }

        private static void StartPublisher(string topic)
        {
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(topic, "fanout", true);


                long counter = 0;
                while (true)
                {
                    counter++;
                    // prepare response queue
                    var replyQueue = channel.QueueDeclare();
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(replyQueue.QueueName, true, consumer);



                    var message = string.Format("Serial NO: #{0:00000}", counter);
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

                    Console.WriteLine();
                    Console.WriteLine("Sent: {0}", message);


                    BasicDeliverEventArgs ea;
                    TimeSpan timeout = TimeSpan.FromSeconds(5.0);

                    DateTime waitUntil = DateTime.Now + timeout;
                    for(int count = 0; count < 2; count++)
                    {
                        if (consumer.Queue.Dequeue((int)waitUntil.Subtract(DateTime.Now).TotalMilliseconds, out ea) == true)
                        {
                            // assure ea.BasicProperties.CorrelationId == corrId;
                            Console.WriteLine("- return: {0}", Encoding.UTF8.GetString(ea.Body));
                        }
                        else
                        {
                            Console.WriteLine("* RPC timeout.");
                            break;
                        }
                    }
                    Console.WriteLine("* Message process complete.");
                    channel.QueueDelete(replyQueue.QueueName);
                    

                    Task.Delay(1000).Wait();
                }
            }
        }
    }
}
