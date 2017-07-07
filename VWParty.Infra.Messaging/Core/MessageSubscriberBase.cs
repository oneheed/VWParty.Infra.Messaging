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
using VWParty.Infra.LogTracking;

namespace VWParty.Infra.Messaging.Core
{
    public enum WorkerStatusEnum : int
    {
        STOPPED,
        STARTING,
        STARTED,
        STOPPING
    }

    public abstract class MessageSubscriberBase<TInputMessage, TOutputMessage> : IDisposable
        where TInputMessage : InputMessageBase
        where TOutputMessage : OutputMessageBase, new()
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();



        
        public WorkerStatusEnum Status {
            get
            {
                return this._status;
            }
            set
            {
                if (this._status != value) _logger.Info("worker status was changed: {0}", value);
                this._status = value;
            }
        } private WorkerStatusEnum _status = WorkerStatusEnum.STOPPED;

        internal string QueueName { get; set; }

        internal IConnection _connection = null;

        protected MessageSubscriberBase(string queueName)
        {
            this.QueueName = queueName;
            // this._connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection();

        }

        public delegate TOutputMessage SubscriberProcess(TInputMessage message, LogTrackerContext logtracker);






        protected virtual string ConnectionName
        {
            get
            {
                return this.GetType().FullName;
            }
        }
        

        private bool _stop = true;

        private bool _is_restart = false;

        



        private object _sync_root = new object();


        public virtual async Task StartWorkersAsync(SubscriberProcess process)
        {
            await this.StartWorkersAsync(
                process,
                1,
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime);
        }

        public virtual async Task StartWorkersAsync(SubscriberProcess process, int worker_count)
        {
            await this.StartWorkersAsync(
                process,
                worker_count,
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime);
        }




        protected virtual async Task StartWorkersAsync(SubscriberProcess process, int worker_count, int retry_count, TimeSpan retry_timeout)
        {
            lock (this._sync_root)
            {
                //if (this._worker_running) throw new InvalidOperationException("worker already started.");
                //this._worker_running = true;
                if (this.Status != WorkerStatusEnum.STOPPED) throw new InvalidOperationException();
                this.Status = WorkerStatusEnum.STARTING;
            }

            //模擬測試 connection 建立過久，導致 worker 長時間處於 starting 狀態的問題。這狀態下呼叫 stopworker 會引發 exception
            //Task.Delay(5000).Wait();

            ConnectionFactory cf = MessageBusConfig.DefaultConnectionFactory;

            while (retry_count > 0)
            {
                try
                {
                    this._connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection(cf.HostName.Split(','), this.ConnectionName);
                }
                catch
                {
                    retry_count--;
                    _logger.Warn("connection create fail. restarting...");
                    Task.Delay(retry_timeout).Wait();
                    continue;
                }

                try
                {
                    // TASK implement

                    //this._stop = false;
                    //this._is_restart = false;
                    //Task[] tasks = new Task[worker_count];

                    //for (int index = 0; index < worker_count; index++)
                    //{
                    //    tasks[index] = Task.Run(() => { this.StartProcessSubscribedMessage(process); });
                    //}

                    //this.Status = WorkerStatusEnum.STARTED;
                    //foreach (Task t in tasks) await t;
                    //this.Status = WorkerStatusEnum.STOPPED;

                    this._stop = false;
                    this._is_restart = false;
                    //Task[] tasks = new Task[worker_count];
                    Thread[] threads = new Thread[worker_count];

                    for (int index = 0; index < worker_count; index++)
                    {
                        //tasks[index] = Task.Run(() => { this.StartProcessSubscribedMessage(process); });
                        threads[index] = new Thread(() => { this.StartProcessSubscribedMessage(process); });
                        threads[index].Start();
                    }

                    this.Status = WorkerStatusEnum.STARTED;

                    //foreach (Task t in tasks) await t;
                    _logger.Info("waiting all worker threads to STOP...");
                    await Task.Run(() => { foreach (Thread t in threads) t.Join(); });

                    this.Status = WorkerStatusEnum.STOPPED;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "waiting task(s) exception. shutdown and retry...");
                    this._is_restart = true;
                    this._stop = true;
                }

                if (this._is_restart == false) break;
                retry_count--;
                await Task.Delay(retry_timeout);
                _logger.Warn("connection fail, restarting...");
            }

            lock (this._sync_root)
            {
                //this._worker_running = false;
                this.Status = WorkerStatusEnum.STOPPED;
            }

            if (this._is_restart && retry_count == 0) throw new Exception("Retry 次數已超過，不再重新嘗試連線。");
        }

        

        public virtual void StopWorkers()
        {
            switch (this.Status)
            {
                case WorkerStatusEnum.STARTED:
                    this.Status = WorkerStatusEnum.STOPPING;
                    this._stop = true;
                    break;

                case WorkerStatusEnum.STARTING:
                    throw new InvalidOperationException("Worker is starting, you can not stop it now.");

                case WorkerStatusEnum.STOPPING:
                case WorkerStatusEnum.STOPPED:
                default:
                    // do nothing
                    break;
            }
        }


        private void StartProcessSubscribedMessage(SubscriberProcess process)
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
                        if (!_connection.IsOpen || channel.IsClosed)
                        {
                            _logger.Trace("WorkerThread({0}) message channel is closed. Reason: {1}", Thread.CurrentThread.ManagedThreadId,
                                _connection.IsOpen ? channel.CloseReason : _connection.CloseReason);
                        }

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

                    LogTrackerContext logtracker = null;

                    if (props.Headers == null || props.Headers[LogTrackerContext._KEY_REQUEST_ID] == null)
                    {
                        // message without logtracker context info
                        logtracker = LogTrackerContext.Create(this.GetType().FullName, LogTrackerContextStorageTypeEnum.NONE);
                    }
                    else
                    {
                        string req_id = Encoding.UTF8.GetString((byte[])props.Headers[LogTrackerContext._KEY_REQUEST_ID]);
                        DateTime req_time = DateTime.Parse(Encoding.UTF8.GetString((byte[])props.Headers[LogTrackerContext._KEY_REQUEST_START_UTCTIME])).ToUniversalTime();

                        logtracker = LogTrackerContext.Init(
                            LogTrackerContextStorageTypeEnum.NONE,
                            req_id,
                            req_time);
                    }
                    
                    try
                    {
                        TInputMessage request = JsonConvert.DeserializeObject<TInputMessage>(Encoding.Unicode.GetString(body));
                        _logger.Trace("WorkerThread({0}) - before processing message: {1}", Thread.CurrentThread.ManagedThreadId, props.MessageId);
                        response = process(request, logtracker);

                        _logger.Trace("WorkerThread({0}) - message was processed: {1}", Thread.CurrentThread.ManagedThreadId, props.MessageId);
                    }
                    catch (Exception e)
                    {
                        response = new TOutputMessage()
                        {
                            exception = e.ToString()
                        };
                        _logger.Warn(e, "WorkerThread({0}) - process message with exception: {1}, ex: {2}", Thread.CurrentThread.ManagedThreadId, props.MessageId, e);
                    }


                    try
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
                    catch(Exception ex)
                    {
                        // connection fail while return message or ack.
                        // just shutdown and ignore it. non-return ack will be ignore, message will retry in next time
                        if (!_connection.IsOpen || channel.IsClosed)
                        {
                            _logger.Trace("WorkerThread({0}) message channel is closed. Reason: {1}", Thread.CurrentThread.ManagedThreadId,
                                _connection.IsOpen ? channel.CloseReason : _connection.CloseReason);
                        }

                        this._stop = true;
                        this._is_restart = true;
                        _logger.Warn(ex, "dequeue exception, restart subscriber...");

                        result = null;
                        break;
                    }
                }

            }

            _logger.Info("WorkerThread({0}) - {1} (restarting: {2}).", Thread.CurrentThread.ManagedThreadId, this.Status, this._is_restart); //this._stop?"stopped":"restarting");

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
