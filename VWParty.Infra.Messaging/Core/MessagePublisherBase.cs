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

        internal protected bool IsWaitReturn { get; set; }

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


        //internal protected virtual async Task<TOutputMessage> PublishMessageAsync(string routing, TInputMessage message, LogTrackerContext tracker)
        //{
        //    return await this.PublishMessageAsync(
        //        this.IsWaitReturn,
        //        MessageBusConfig.DefaultWaitReplyTimeOut,
        //        MessageBusConfig.DefaultMessageExpirationTimeout,
        //        routing,
        //        message,
        //        MessageBusConfig.DefaultRetryCount,
        //        MessageBusConfig.DefaultRetryWaitTime,
        //        tracker);
        //}

        //internal protected virtual async Task<TOutputMessage> PublishMessageAsync(string routing, byte[] message, LogTrackerContext tracker)
        //{
        //    return await this.PublishMessageAsync(
        //        this.IsWaitReturn,
        //        MessageBusConfig.DefaultWaitReplyTimeOut,
        //        MessageBusConfig.DefaultMessageExpirationTimeout,
        //        routing,
        //        message,
        //        null,
        //        MessageBusConfig.DefaultRetryCount,
        //        MessageBusConfig.DefaultRetryWaitTime,
        //        tracker);
        //}
        

        internal protected virtual async Task<TOutputMessage> PublishMessageAsync(
            bool isWaitReply,
            TimeSpan waitReplyTimeout,
            TimeSpan messageExpirationTimeout,
            string routing,
            TInputMessage message,
            int retryCount,
            TimeSpan retryWaitTimeout,
            LogTrackerContext tracker)
        {
            return await this.PublishMessageAsync(
                isWaitReply,
                waitReplyTimeout,
                messageExpirationTimeout,
                routing,
                Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message)),
                new Dictionary<string, object>() {
                    {"INPUT-MESSAGE-TYPE", typeof(TInputMessage).FullName },
                    {"OUTPUT-MESSAGE-TYPE", typeof(TOutputMessage).FullName }
                },
                retryCount,
                retryWaitTimeout,
                tracker);
        }


        /// <summary>
        /// NOTE:
        /// 所有的 PublishMessageAsync( ) 系列 overloading method 裡，這個 method 是真正在處理訊息發送的部分。
        /// 其餘的 overloading 目的只是做預設參數的處理
        /// </summary>
        /// <param name="isWaitReply"></param>
        /// <param name="waitReplyTimeout"></param>
        /// <param name="messageExpirationTimeout"></param>
        /// <param name="routing"></param>
        /// <param name="messageBody"></param>
        /// <param name="messageHeaders"></param>
        /// <param name="retryCount"></param>
        /// <param name="retryWaitTimeout"></param>
        /// <param name="tracker"></param>
        /// <returns></returns>
        internal protected virtual async Task<TOutputMessage> PublishMessageAsync(
            bool isWaitReply,
            TimeSpan waitReplyTimeout,
            TimeSpan messageExpirationTimeout,
            string routing,
            //TInputMessage message,
            byte[] messageBody,
            Dictionary<string, object> messageHeaders,
            int retryCount,
            TimeSpan retryWaitTimeout,
            LogTrackerContext tracker)
        {

            bool isPublishComplete = false;
            ConnectionFactory connFac = MessageBusConfig.DefaultConnectionFactory;

            while (true)
            {
                if (retryCount <= 0)
                {
                    // 已經超出最大的重新連線嘗試次數。
                    throw new Exception("RetryLimitException");
                }

                try
                {
                    using (var connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection(connFac.HostName.Split(','), this.ConnectionName))
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


                        if (isWaitReply)
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

                        if (tracker == null)
                        {
                            tracker = LogTrackerContext.Create(
                                this.GetType().Name, 
                                LogTrackerContextStorageTypeEnum.NONE);
                        }

                        props.Headers = (messageHeaders == null)?
                            new Dictionary<string, object>() :
                            new Dictionary<string, object>(messageHeaders);

                        props.Headers[LogTrackerContext._KEY_REQUEST_ID] = tracker.RequestId;
                        props.Headers[LogTrackerContext._KEY_REQUEST_START_UTCTIME] = tracker.RequestStartTimeUTC_Text;

                        if (isWaitReply)
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
                            isPublishComplete = true;

                            if (isWaitReply)
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
                            if (isWaitReply && string.IsNullOrEmpty(replyQueueName) == false)
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
                    if (isPublishComplete) throw;

                    // connection fail.
                    _logger.Warn(ex, "Retry (left: {0}) ...", retryCount);
                    //Console.WriteLine("Retry..");
                    retryCount--;
                    await Task.Delay(retryWaitTimeout);
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
