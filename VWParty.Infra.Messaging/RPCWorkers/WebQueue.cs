using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.Messaging.Core;

namespace VWParty.Infra.Messaging.RPCWorkers
{
    public class WebQueue : MessagePublisherBase<RequestMessage, ResponseMessage>//, IDisposable
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        //private string QueueName = null;

        public WebQueue(string queueName): 
            base(queueName)
            
        {
            //this.QueueName = queueName;
            //_logger.Info("create webqueue connection - host: {0}, port: {1}", factory.HostName, factory.Port);
        }

        //public string Call(string message)
        public ResponseMessage CallWorkerProcess(RequestMessage request)
        {
            return this.CallWorkerProcess(request, TimeSpan.FromSeconds(10));
        }
        public ResponseMessage CallWorkerProcess(RequestMessage request, TimeSpan messageExpirationTimeout)
        {
            return this.PublishAndWaitResponseMessage(
                true,
                TimeSpan.FromSeconds(10),
                messageExpirationTimeout,
                //this.QueueName,
                this.QueueName,
                request);
        }



        //public void Dispose()
        //{
        //    _logger.Info("close webqueue connection.");
        //}
    }







/*
    public class _WebQueue
    {
        ////private static readonly TimeSpan TimeToBeReceived = TimeSpan.FromMinutes(15.0);
        //private string[] names = new string[]
        //{
        //    "data",
        //    "ack",
        //    "return"
        //};

        //private Dictionary<string, MessageQueue> queues = new Dictionary<string, MessageQueue>();

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        //public WebQueue(string dataQueuePath, string ackQueuePath, string returnQueuePath)
        //{
        //    this.queues["data"] = new MessageQueue(dataQueuePath);
        //    this.queues["ack"] = new MessageQueue(ackQueuePath);
        //    this.queues["return"] = new MessageQueue(returnQueuePath);

        //    foreach (string name in this.names)
        //    {
        //        this.queues[name].Formatter = QueueConfig.MessageFormatter;
        //        this.queues[name].MessageReadPropertyFilter = QueueConfig.MessageFilter;
        //    }
        //}


        private ConnectionFactory connectionFactory = null;
        //private IConnection connection = null;

        //const string exchangeTopic = "tp-transaction";
        private string _rpcQueueName = null;

        [Obsolete]
        public _WebQueue() : this("10.101.6.173", 5672)
        {

        }

        public _WebQueue(string hostname, int port, string queueName)
        {
            this.connectionFactory = new ConnectionFactory()
            {
                HostName = hostname,
                Port = port
            };

            this._rpcQueueName = queueName;
        }

        #region for client API(s)

        public ResponseMessage CallWorkerProcess(RequestMessage request)
        //{
        //    string msgid = this.SendMessage(request, TimeSpan.FromMinutes(5.0));
        //    //this.ReceiveAcknowledge(msgid, TimeSpan.FromMinutes(30.0));
        //    ResponseMessage response = this.ReceiveResponse(msgid, TimeSpan.FromMinutes(30.0));

        //    if (string.IsNullOrEmpty(response.exception) == false)
        //    {
        //        throw new Exception("remote call exception: " + response.exception);
        //    }

        //    return response;
        //}

        //public delegate object process_subscribed_message(BetTransactionMessage betmsg);


        //public string SendMessage(RequestMessage request, TimeSpan maxTimeToBeReceived)
        {
            TimeSpan maxTimeToBeReceived = TimeSpan.FromMinutes(5.0);

            using (var connection = this.connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                //string exchangeName = "tp-transaction";
                //string brandId = "letou";

                //channel.ExchangeDeclare(
                //    exchangeTopic,
                //    "direct",
                //    true,
                //    false,
                //    null);
                var rpcQueue = channel.QueueDeclare(
                    this._rpcQueueName,
                    true,
                    false,
                    false,
                    null);

                var replyQueue = channel.QueueDeclare();

                try
                {
                    // prepare response queue
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(replyQueue.QueueName, true, consumer);



                    //var message = "-------------------------------------";
                    var corrId = Guid.NewGuid().ToString("N");

                    IBasicProperties props = channel.CreateBasicProperties();
                    props.ContentType = "application/json";
                    //props.DeliveryMode = 2;
                    //props.Expiration = "10000"; // per message expiration

                    // setup return mechanism
                    props.ReplyTo = replyQueue.QueueName;
                    props.CorrelationId = corrId;

                    
                    channel.BasicPublish(
                        exchangeTopic,
                        null,
                        props,
                        Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(request)));

                    //Console.WriteLine();
                    //Console.WriteLine("Sent: {0}", message);
                    //return;

                    BasicDeliverEventArgs ea;
                    if (consumer.Queue.Dequeue((int)maxTimeToBeReceived.TotalMilliseconds, out ea) == true)
                    {
                        //assure ea.BasicProperties.CorrelationId == corrId;

                        ResponseMessage response = JsonConvert.DeserializeObject<ResponseMessage>(Encoding.Unicode.GetString(ea.Body));
                        return response;
                    }
                    else
                    {
                        //Console.WriteLine("* RPC timeout.");
                        //break;
                        throw new TimeoutException();
                    }
                }
                finally
                {
                    channel.QueueDelete(replyQueue.QueueName);
                }
            }
        }

        //[Obsolete("Do NOT support in rabbitmq mode", true)]
        //public Message ReceiveAcknowledge(string correlationID, TimeSpan waitTimeout)
        //{
        //    //return this.queues["ack"].ReceiveByCorrelationId(correlationID, waitTimeout);
        //}

        //[Obsolete("Do NOT support in rabbitmq mode", true)]
        //public ResponseMessage ReceiveResponse(string correlationID, TimeSpan waitTimeout)
        //{
        //    //this.queues["return"].MessageReadPropertyFilter = QueueConfig.MessageFilter;
        //    //return this.queues["return"].ReceiveByCorrelationId(correlationID, waitTimeout).Body as ResponseMessage;
        //}




        //[Obsolete("Do NOT support in rabbitmq mode", true)]
        //public int IsReturned(string correlationID)
        //{
        //    int status = 0;
        //    try
        //    {
        //        var ackmsg = this.queues["ack"].PeekByCorrelationId(correlationID);
        //        status = 1;

        //        //Console.Write("[{0}]", ackmsg.Acknowledgment);

        //        this.queues["return"].PeekByCorrelationId(correlationID);
        //        status = 2;
        //    }
        //    catch (Exception ex)
        //    {
        //        //Console.WriteLine(ex);
        //    }
        //    return status;
        //}
        #endregion



    }

*/


}
