using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging.Core
{
    public class MessageSubscriberBase<TInputMessage, TOutputMessage> : IDisposable
        where TInputMessage : InputMessageBase
        where TOutputMessage : OutputMessageBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();


        internal string QueueName { get; set; }

        internal IConnection _connection = null;

        protected MessageSubscriberBase(string queueName)
        {
            this.QueueName = queueName;
            this._connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection();
        }

        public delegate TOutputMessage SubscriberProcess(TInputMessage message);



        private Task[] running_worker_tasks = null;
        private bool _stop = true;


        public virtual void StartWorkers(SubscriberProcess process)
        {
            this.StartWorkers(process, 1);
        }
        public virtual void StartWorkers(SubscriberProcess process, int worker_count)
        {
            _stop = false;
            Task[] tasks = new Task[worker_count];

            for (int index = 0; index < worker_count; index++)
            {
                tasks[index] = Task.Run(() => { this.StartProcessSubscribedMessage(process); });
            }

            this.running_worker_tasks = tasks;
        }

        public virtual void Stop()
        {
            this._stop = true;
            //this._wait.WaitOne();
            Task.WaitAll(this.running_worker_tasks);
        }


        protected virtual void StartProcessSubscribedMessage(SubscriberProcess process)
        {
            //this._stop = false;

            //Stopwatch totalWatch = new Stopwatch();
            //totalWatch.Start();
            _logger.Info("WorkerThread({0}) - start running...", Thread.CurrentThread.ManagedThreadId);

            //using (var connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection()) //this.connectionFactory.CreateConnection())
            using (var channel = this._connection.CreateModel())
            {
                channel.QueueDeclare(
                    queue: this.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                //channel.BasicQos(0, 1, false);
                //var consumer = new EventingBasicConsumer(channel);
                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(
                    queue: this.QueueName,
                    noAck: false,
                    consumer: consumer);

                while (this._stop == false)
                {
                    BasicDeliverEventArgs ea;
                    if (consumer.Queue.Dequeue(100, out ea) == false) continue;

                    _logger.Info("WorkerThread({0}) - receive message: {1}", Thread.CurrentThread.ManagedThreadId, ea.BasicProperties.MessageId);

                    TOutputMessage response = null;

                    var body = ea.Body;
                    var props = ea.BasicProperties;

                    bool current_reply = (string.IsNullOrEmpty(props.ReplyTo) == false);

                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        TInputMessage request = JsonConvert.DeserializeObject<TInputMessage>(Encoding.Unicode.GetString(body));

                        _logger.Info("WorkerThread({0}) - before processing message: {1}", Thread.CurrentThread.ManagedThreadId, props.MessageId);
                        //this.CurrentZeusRequestId = request.request_id;
                        response = process(request);
                        _logger.Info("WorkerThread({0}) - message was processed: {1}", Thread.CurrentThread.ManagedThreadId, props.MessageId);

                    }
                    catch (Exception e)
                    {
                        response = default(TOutputMessage);
                        response.exception = e.ToString();
                        _logger.Warn(e, "WorkerThread({0}) - process message with exception: {1}, ex: {2}", Thread.CurrentThread.ManagedThreadId, props.MessageId, e);
                    }
                    finally
                    {
                        if (current_reply)
                        {
                            var responseBytes = //Encoding.UTF8.GetBytes(response);
                                Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));

                            channel.BasicPublish(
                                exchange: "",
                                routingKey: props.ReplyTo,
                                basicProperties: replyProps,
                                body: responseBytes);
                        }
                    
                        channel.BasicAck(
                            deliveryTag: ea.DeliveryTag,
                             multiple: false);
                    }
                }

            }

            //totalWatch.Stop();
            //_logger.Info("WorkerThread({0}) - stopped (elapsed seconds: {1})", Thread.CurrentThread.ManagedThreadId, totalWatch.Elapsed.TotalSeconds.ToString("0.000"));
            _logger.Info("WorkerThread({0}) - stopped.", Thread.CurrentThread.ManagedThreadId);

        }

        public void Dispose()
        {
            if (this._connection != null)
            {
                this._connection.Close();
            }
        }
    }
}
