using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging.RPCWorkers
{
    public class WorkerQueue : IDisposable
    {
        public delegate ResponseMessage WorkerProcess(RequestMessage request);

        private IConnection connection;

        private string _rpcQueueName = null;

        public WorkerQueue(string queueName)
        {
            //ConnectionFactory factory = new ConnectionFactory()
            //{
            //    //HostName = "10.101.6.173",
            //    //Port = 5672
            //    HostName = QueueConfig.QueueHostName,
            //    Port = QueueConfig.QueuePortNumber
            //};

            connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection();

            this._rpcQueueName = queueName;
        }



        private void Start(WorkerProcess process)
        {
            this._stop = false;

            using (var channel = connection.CreateModel())
            {
 

                channel.QueueDeclare(
                    queue: this._rpcQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                //channel.BasicQos(0, 1, false);
                //var consumer = new EventingBasicConsumer(channel);
                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(
                    queue: this._rpcQueueName,
                    noAck: false,
                    consumer: consumer);

                while(this._stop == false)
                {
                    //var ea = consumer.Queue.Dequeue();
                    BasicDeliverEventArgs ea;
                    if (consumer.Queue.Dequeue(100, out ea) == false) continue;



                    ResponseMessage response = null;

                    var body = ea.Body;
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        RequestMessage request = JsonConvert.DeserializeObject<RequestMessage>(Encoding.Unicode.GetString(body));

                        Console.Write("received message({0})...", request.input_json);
                        response = process(request);
                        Console.WriteLine("OK");
                    }
                    catch (Exception e)
                    {
                        response = new ResponseMessage()
                        {
                            exception = e.ToString()
                        };
                    }
                    finally
                    {
                        var responseBytes = //Encoding.UTF8.GetBytes(response);
                            Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                        channel.BasicPublish(
                            exchange: "", 
                            routingKey: props.ReplyTo,
                            basicProperties: replyProps, 
                            body: responseBytes);
                        channel.BasicAck(
                            deliveryTag: ea.DeliveryTag,
                             multiple: false);
                    }
                }
                
            }
        }




        public void Dispose()
        {
            connection.Close();
        }






        [Obsolete]
        public Task StartAsync(WorkerProcess process)
        {
            this.StartWorkers(process, 1);
            return this.running_worker_tasks[0];
        }

        public void StartWorkers(WorkerProcess process)
        {
            this.StartWorkers(process, 1);
        }
        public void StartWorkers(WorkerProcess process, int worker_count)
        {
            Task[] tasks = new Task[worker_count];

            for (int index = 0; index < worker_count; index++)
            {
                tasks[index] = Task.Run(() => { this.Start(process); });
            }

            this.running_worker_tasks = tasks;
        }

        private Task[] running_worker_tasks = null;
        private bool _stop = true;


        public void Stop()
        {
            this._stop = true;
            //this._wait.WaitOne();
            Task.WaitAll(this.running_worker_tasks);
        }

        public void WaitExit()
        {
            //this._wait.WaitOne();
            Task.WaitAll(this.running_worker_tasks);
        }
    }


/*
    public class _WorkerQueue
    {
        public string CurrentZeusRequestId
        {
            get
            {
                //Thread.SetData(Thread.GetNamedDataSlot("ZeusRequestID"), "123123123123");
                return Thread.GetData(Thread.GetNamedDataSlot("ZeusRequestID")) as string;
            }
            private set
            {
                Thread.SetData(Thread.GetNamedDataSlot("ZeusRequestId"), value);
            }
        }
        private string[] names = new string[]
        {
            "data",
            //"retry"
        };

        public delegate ResponseMessage WorkerProcess(RequestMessage request);

        //private Dictionary<string, MessageQueue> queues = new Dictionary<string, MessageQueue>();

        private static Logger _logger = LogManager.GetCurrentClassLogger();


        //public WorkerQueue(string dataQueuePath) //, string retryQueuePath)
        //{
        //    this.queues["data"] = new MessageQueue(dataQueuePath);
        //    //this.queues["retry"] = new MessageQueue(retryQueuePath);

        //    foreach (string name in this.names)
        //    {
        //        this.queues[name].Formatter = QueueConfig.MessageFormatter;
        //        this.queues[name].MessageReadPropertyFilter = QueueConfig.MessageFilter;
        //    }
        //}


        //#region for worker API(s)
        //public RequestMessage ReceiveMessage(TimeSpan waitTimeout)
        //{
        //    Message msg = this.queues["data"].Receive();
        //    RequestMessage req = msg.Body as RequestMessage;

        //    req.CorrelationId = msg.Id;
        //    req.retryCount--;

        //    return req;
        //}

        //public object ReturnMessage(string messageid, object body)
        //{
        //    throw new NotImplementedException();
        //}
        //#endregion


        private bool _stop = true;
        

        //private ManualResetEvent _wait = new ManualResetEvent(false);


        [Obsolete]
        public Task StartAsync(WorkerProcess process)
        {
            this.StartWorkers(process, 1);
            return this.running_worker_tasks[0];
        }

        public void StartWorkers(WorkerProcess process)
        {
            this.StartWorkers(process, 1);
        }
        public void StartWorkers(WorkerProcess process, int worker_count)
        {
            Task[] tasks = new Task[worker_count];

            for (int index = 0; index < worker_count; index++)
            {
                tasks[index] = Task.Run(() => { this.Start(process); });
            }

            this.running_worker_tasks = tasks;
        }

        private Task[] running_worker_tasks = null;


        private void Start(WorkerProcess process)
        {
            this._stop = false;
            TimeSpan wait = TimeSpan.FromSeconds(1.0);
            //this._wait.Reset();


            _logger.Info("Worker({0}) - start running...", Thread.CurrentThread.ManagedThreadId);
            while (this._stop == false)
            {
                Message msg = null;
                try
                {
                    this.queues["data"].MessageReadPropertyFilter = QueueConfig.MessageFilter;
                    msg = this.queues["data"].Receive(wait);
                    _logger.Info("Worker({0}) - receive message: {1}", Thread.CurrentThread.ManagedThreadId, msg.Id);
                }
                catch (MessageQueueException mqex)
                {
                    // timeout
                    continue;
                }


                RequestMessage request = null;
                ResponseMessage response = null;
                try
                {
                    request = msg.Body as RequestMessage;
                    request.CorrelationId = request.CorrelationId ?? msg.Id;
                    request.retryCount--;
                    _logger.Info("Worker({0}) - before processing message: {1}", Thread.CurrentThread.ManagedThreadId, msg.Id);
                    this.CurrentZeusRequestId = request.request_id;
                    response = process(request);
                    _logger.Info("Worker({0}) - message was processed: {1}", Thread.CurrentThread.ManagedThreadId, msg.Id);

                    if (response == null) throw new ArgumentNullException("response");
                    
                    if (msg.ResponseQueue != null)
                    {
                        Message rspmsg = new Message();
                        rspmsg.CorrelationId = request.CorrelationId;
                        rspmsg.Formatter = QueueConfig.MessageFormatter; 
                        rspmsg.Body = response;
                        msg.ResponseQueue.Send(rspmsg, MessageQueueTransactionType.None);
                    }
                }
                catch(Exception ex)
                {
                    //Console.WriteLine("Prcesss Message Exception: {0}", ex);
                    _logger.Warn(ex, "Worker({0}) - process message with exception: {1}, ex: {2}", Thread.CurrentThread.ManagedThreadId, msg.Id, ex);


                    // NO retry

                    // do retry
                    //if (request.retryCount > 0)
                    //{
                    //    //msg.Priority = MessagePriority.VeryLow;
                    //    //this.queues["data"].Send(msg);

                    //    Message myMessage = new Message();
                    //    myMessage.CorrelationId = request.CorrelationId;
                    //    myMessage.Formatter = QueueConfig.MessageFormatter; //this.queues["data"].Formatter;
                    //    myMessage.Body = request;
                    //    myMessage.ResponseQueue = msg.ResponseQueue;
                    //    myMessage.Priority = MessagePriority.VeryLow;

                    //    this.queues["data"].Send(myMessage, MessageQueueTransactionType.None);
                    //}
                    //else
                    {
                        // retry 超過次數限制, 透過 response 傳回 exception
                        Message exmsg = new Message();
                        exmsg.CorrelationId = request.CorrelationId;
                        exmsg.Formatter = QueueConfig.MessageFormatter;
                        exmsg.Body = new ResponseMessage()
                        {
                            exception = ex.ToString()
                        };
                        msg.ResponseQueue.Send(exmsg);
                    }
                }
            }

            //this._wait.Set();
            _logger.Info("Worker({0}) - stopped", Thread.CurrentThread.ManagedThreadId);

        }

        public void Stop()
        {
            this._stop = true;
            //this._wait.WaitOne();
            Task.WaitAll(this.running_worker_tasks);
        }

        public void WaitExit()
        {
            //this._wait.WaitOne();
            Task.WaitAll(this.running_worker_tasks);
        }
    }
*/


}
