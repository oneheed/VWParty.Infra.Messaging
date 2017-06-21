using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;

namespace VWParty.Infra.Messaging.Core
{
    //public abstract class MessageOneWayPublisherBase<TInputMessage> : MessagePublisherBase<TInputMessage, OutputMessageBase>
    //    : where TInputMessage : InputMessageBase
    //{

    //}


    public abstract class MessagePublisherBase<TInputMessage, TOutputMessage> : IDisposable
        where TInputMessage : InputMessageBase
        where TOutputMessage : OutputMessageBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        internal bool IsWaitReturn { get; set; }

        //internal string MessageBusConfigName { get; set; }
        internal string ExchangeName { get; set; }

        internal string ExchangeType { get; set; }

        internal string QueueName { get; set; }

        internal MessageBusTypeEnum BusType { get; set; }


        protected MessagePublisherBase(string exchangeName, string exchangeType)
        {
            //this.MessageBusConfigName = config;
            this.BusType = MessageBusTypeEnum.EXCHANGE;
            this.ExchangeName = exchangeName;
            this.ExchangeType = exchangeType;
        }

        protected MessagePublisherBase(string queueName)
        {
            this.BusType = MessageBusTypeEnum.QUEUE;
            this.QueueName = queueName;
        }


        protected virtual string ConnectionName
        {
            get
            {
                return this.GetType().FullName;
            }
        }


        internal protected virtual async Task<TOutputMessage> PublishMessageAsync(string routing, TInputMessage message, LogTrackerContext tracker)
        {
            return await this.PublishMessageAsync(
                this.IsWaitReturn,
                TimeSpan.FromSeconds(15),
                TimeSpan.FromMinutes(30),
                routing,
                message,
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime,
                tracker);
        }
        internal protected virtual async Task<TOutputMessage> PublishMessageAsync(string routing, byte[] message, LogTrackerContext tracker)
        {
            return await this.PublishMessageAsync(
                this.IsWaitReturn,
                TimeSpan.FromSeconds(15),
                TimeSpan.FromMinutes(30),
                routing,
                message,
                null,
                MessageBusConfig.DefaultRetryCount,
                MessageBusConfig.DefaultRetryWaitTime,
                tracker);
        }


        protected virtual async Task<TOutputMessage> PublishMessageAsync(
            bool reply,
            TimeSpan waitReplyTimeout,
            TimeSpan messageExpirationTimeout,
            string routing,
            TInputMessage message,
            int retry_count,
            TimeSpan retryWait,
            LogTrackerContext logtracker)
        {
            return await this.PublishMessageAsync(
                reply,
                waitReplyTimeout,
                messageExpirationTimeout,
                routing,
                Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message)),
                null,
                retry_count,
                retryWait,
                logtracker);
        }

        protected virtual async Task<TOutputMessage> PublishMessageAsync(
            bool reply,
            TimeSpan waitReplyTimeout,
            TimeSpan messageExpirationTimeout,
            string routing,
            //TInputMessage message,
            byte[] messageBody,
            Dictionary<string, object> messageHeaders,
            int retry_count,
            TimeSpan retryWait,
            LogTrackerContext logtracker)
        {

            bool is_sent_complete = false;
            //bool is_receive_complete = false;
            //string correlationId = Guid.NewGuid().ToString("N");

            //using (var connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection())
            ConnectionFactory cf = MessageBusConfig.DefaultConnectionFactory;

            //while(retry_count > 0)
            while (true)
            {
                if (retry_count <= 0)
                {
                    // 已經超出最大的重新連線嘗試次數。
                    throw new Exception("RetryLimitException");
                }

                try
                {
                    using (var connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection(cf.HostName.Split(','), this.ConnectionName))
                    using (var channel = connection.CreateModel())
                    {
                        string replyQueueName = null;

                        if (this.BusType == MessageBusTypeEnum.QUEUE)
                        {
                            channel.QueueDeclare(
                                //queue: routing,
                                queue: this.QueueName,
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
                        }
                        else if (this.BusType == MessageBusTypeEnum.EXCHANGE)
                        {
                            channel.ExchangeDeclare(
                                this.ExchangeName,
                                this.ExchangeType,
                                true,
                                false,
                                null);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }


                        if (reply)
                        {
                            replyQueueName = channel.QueueDeclare().QueueName;
                            //consumer = new QueueingBasicConsumer(channel);
                            //channel.BasicConsume(
                            //    queue: replyQueueName,
                            //    noAck: true,
                            //    consumer: consumer);
                        }



                        IBasicProperties props = channel.CreateBasicProperties();
                        props.ContentType = "application/json";
                        props.Expiration = messageExpirationTimeout.TotalMilliseconds.ToString();

                        if (logtracker == null)
                        {
                            logtracker = LogTrackerContext.Create(LogTrackerContextStorageTypeEnum.NONE);
                        }

                        props.Headers = (messageHeaders == null)?
                            new Dictionary<string, object>() :
                            new Dictionary<string, object>(messageHeaders);

                        props.Headers[LogTrackerContext._KEY_REQUEST_ID] = logtracker.RequestId;
                        props.Headers[LogTrackerContext._KEY_REQUEST_START_UTCTIME] = logtracker.RequestStartTimeUTC_Text;

                        if (reply)
                        {
                            //message.correlationId =
                            props.CorrelationId = Guid.NewGuid().ToString("N");
                            props.ReplyTo = replyQueueName;
                        }


                        try
                        {
                            if (this.BusType == MessageBusTypeEnum.EXCHANGE)
                            {
                                channel.BasicPublish(
                                    exchange: this.ExchangeName ?? "",
                                    routingKey: routing,
                                    basicProperties: props,
                                    //body: Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message)));
                                    body: messageBody);
                            }
                            else if (this.BusType == MessageBusTypeEnum.QUEUE)
                            {
                                channel.BasicPublish(
                                    exchange: "",
                                    routingKey: this.QueueName,
                                    basicProperties: props,
                                    //body: Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message)));
                                    body: messageBody);
                            }
                            is_sent_complete = true;

                            if (reply)
                            {
                                BasicGetResult result = null;
                                DateTime until = DateTime.Now.Add(waitReplyTimeout);
                                while (result == null && DateTime.Now < until)
                                {
                                    result = channel.BasicGet(replyQueueName, true);
                                    if (result == null) Task.Delay(MessageBusConfig.DefaultPullWaitTime).Wait();
                                }

                                if (result != null)
                                {
                                    // done, receive response message
                                    TOutputMessage response = JsonConvert.DeserializeObject<TOutputMessage>(Encoding.Unicode.GetString(result.Body));

                                    return response;
                                }
                                else
                                {
                                    // timeout, do not wait anymore
                                    throw new TimeoutException(string.Format(
                                        "MessageBus 沒有在訊息指定的時間內 ({0}) 收到 ResponseMessage 回覆。",
                                        waitReplyTimeout));
                                }
                            }

                            break;
                        }
                        finally
                        {
                            if (reply && string.IsNullOrEmpty(replyQueueName) == false)
                            {
                                channel.QueueDelete(replyQueueName);
                            }
                        }

                    }
                }
                catch(Exception ex)
                {
                    // 如果訊息已經成功送出，只是在等待 response 時發生連線錯誤，則不會進行 Retry, 會原封不動地把 exception 丟回給呼叫端。
                    // 由呼叫端決定該如何進行下一個步驟。
                    if (is_sent_complete) throw;

                    // connection fail.
                    _logger.Warn(ex, "Retry (left: {0}) ...", retry_count);
                    //Console.WriteLine("Retry..");
                    retry_count--;
                    await Task.Delay(retryWait);
                    continue;
                }
                finally
                {
                    
                }
            }


            return null;
        }

        public void Dispose()
        {

        }
    }

}
