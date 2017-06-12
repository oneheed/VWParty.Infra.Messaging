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
            // this._connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection();

        }

        public delegate TOutputMessage SubscriberProcess(TInputMessage message);


        [Obsolete]
        private Task[] running_worker_tasks = null;

        private bool _stop = true;
        //private bool _restart = true;

        private bool _is_restart = false;


        public virtual void StartWorkers(SubscriberProcess process)
        {
            this.StartWorkersAsync(process, 1).Wait();
        }

        public virtual void StartWorkers(SubscriberProcess process, int worker_count)
        {
            this.StartWorkersAsync(process, worker_count).Wait();
        }

        public virtual async Task StartWorkersAsync(SubscriberProcess process, int worker_count)
        {
            await this.StartWorkersAsync(
                process,
                worker_count,
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime);
        }


        private object _sync_root = new object();
        private bool _worker_running = false;

        public virtual async Task StartWorkersAsync(SubscriberProcess process, int worker_count, int retry_count, TimeSpan retry_timeout)
        {
            lock (this._sync_root)
            {
                if (this._worker_running) throw new InvalidOperationException("worker already started.");
                this._worker_running = true;
            }

            while (retry_count > 0)
            {
                ConnectionFactory cf = MessageBusConfig.DefaultConnectionFactory;
                this._connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection(cf.HostName.Split(','));

                _stop = false;
                _is_restart = false;
                Task[] tasks = new Task[worker_count];

                for (int index = 0; index < worker_count; index++)
                {
                    tasks[index] = Task.Run(() => { this.StartProcessSubscribedMessage(process); });
                }

                await Task.Run(() => { Task.WaitAll(tasks); });

                if (this._is_restart == false) break;
                retry_count--;
                await Task.Delay(retry_timeout);
                _logger.Warn("connection fail, restarting...");
            }

            lock (this._sync_root)
            {
                this._worker_running = false;
            }

            if (this._is_restart && retry_count == 0) throw new Exception("retry count limit");
        }


        [Obsolete("use async mode instead. Use StartWorkerAsync / StopWorker API")]
        public virtual void Stop()
        {
            this._stop = true;
            //this._wait.WaitOne();
            Task.WaitAll(this.running_worker_tasks);
        }

        public virtual void StopWorker()
        {
            this._stop = true;
        }

        //public virtual bool Wait(TimeSpan timeout)
        //{
        //    return Task.WaitAll(this.running_worker_tasks, (int)timeout.TotalMilliseconds);
        //}
        //public virtual bool IsNeedRestart
        //{
        //    get
        //    {
        //        return this._is_restart;
        //    }
        //}

        protected virtual void StartProcessSubscribedMessage(SubscriberProcess process)
        {
            //this._stop = false;

            //Stopwatch totalWatch = new Stopwatch();
            //totalWatch.Start();
            _logger.Trace("WorkerThread({0}) - start running...", Thread.CurrentThread.ManagedThreadId);

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
                //var consumer = new QueueingBasicConsumer(channel);

                //channel.BasicConsume(
                //    queue: this.QueueName,
                //    noAck: false,
                //    consumer: consumer);

                BasicGetResult result = null;
                while (this._stop == false)
                {
                    try
                    {
                        result = channel.BasicGet(this.QueueName, false);
                        if (result == null)
                        {
                            Task.Delay(MessageBusConfig.DefaultPullWaitTime).Wait();
                            continue;
                        }
                        _logger.Trace("WorkerThread({0}) - receive message: {1}", Thread.CurrentThread.ManagedThreadId, result.BasicProperties.MessageId);

                    }
                    catch (Exception ex)
                    {
                        this._stop = true;
                        this._is_restart = true;
                        _logger.Warn(ex, "dequeue exception, restart subscriber...");

                        result = null;
                        break;
                    }

                    TOutputMessage response = null;

                    var body = result.Body;
                    var props = result.BasicProperties;

                    bool current_reply = (string.IsNullOrEmpty(props.ReplyTo) == false);

                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        TInputMessage request = JsonConvert.DeserializeObject<TInputMessage>(Encoding.Unicode.GetString(body));

                        _logger.Trace("WorkerThread({0}) - before processing message: {1}", Thread.CurrentThread.ManagedThreadId, props.MessageId);
                        //this.CurrentZeusRequestId = request.request_id;
                        response = process(request);
                        // TODO: 如果 process( ) 出現 exception, 目前的 code 還無法有效處理


                        _logger.Trace("WorkerThread({0}) - message was processed: {1}", Thread.CurrentThread.ManagedThreadId, props.MessageId);

                    }
                    catch (Exception e)
                    {
                        //response = default(TOutputMessage);
                        //response.exception = e.ToString();
                        _logger.Warn(e, "WorkerThread({0}) - process message with exception: {1}, ex: {2}", Thread.CurrentThread.ManagedThreadId, props.MessageId, e);
                    }
                    finally
                    {
                        if (current_reply)
                        {
                            byte[] responseBytes = null;//Encoding.UTF8.GetBytes(response);
                            if (response != null)
                            {
                                responseBytes = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                            }

                            channel.BasicPublish(
                                exchange: "",
                                routingKey: props.ReplyTo,
                                basicProperties: replyProps,
                                body: responseBytes);
                        }
                    
                        channel.BasicAck(
                            deliveryTag: result.DeliveryTag,
                             multiple: false);
                    }
                }

            }

            //totalWatch.Stop();
            //_logger.Trace("WorkerThread({0}) - stopped (elapsed seconds: {1})", Thread.CurrentThread.ManagedThreadId, totalWatch.Elapsed.TotalSeconds.ToString("0.000"));
            _logger.Trace("WorkerThread({0}) - {1}.", Thread.CurrentThread.ManagedThreadId, this._stop?"stopped":"restarting");

        }

        public void Dispose()
        {
            try
            {
                if (this._connection != null)
                {
                    this._connection.Close();
                }
            }
            catch { }
        }
    }
}
