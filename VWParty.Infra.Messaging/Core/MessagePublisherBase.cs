﻿using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging.Core
{
    public class MessagePublisherBase<TInputMessage, TOutputMessage> : IDisposable
        where TInputMessage : InputMessageBase
        where TOutputMessage : OutputMessageBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public bool IsWaitReturn { get; set; }

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



        public virtual void PublishMessage(string routing, TInputMessage message)
        {
            this.PublishAndWaitResponseMessage(
                this.IsWaitReturn,
                TimeSpan.FromSeconds(15),
                TimeSpan.FromMinutes(30),
                routing,
                message);
        }


        protected async Task<TOutputMessage> PublishAndWaitResponseMessageAsync(
            bool reply,
            TimeSpan waitReplyTimeout,
            TimeSpan messageExpirationTimeout,
            string routing,
            TInputMessage message)
        {
            return await Task.Run<TOutputMessage>(() =>
            {
                return this.PublishAndWaitResponseMessage(reply, waitReplyTimeout, messageExpirationTimeout, routing, message);
            });
        }

        protected virtual TOutputMessage PublishAndWaitResponseMessage(
            bool reply,
            TimeSpan waitReplyTimeout,
            TimeSpan messageExpirationTimeout,
            string routing,
            TInputMessage message)
        {
            int retry_count = 3;
            TimeSpan retryWait = TimeSpan.FromSeconds(3);

            //bool is_sent_complete = false;
            //bool is_receive_complete = false;
            //string correlationId = Guid.NewGuid().ToString("N");

            //using (var connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection())
            ConnectionFactory cf = MessageBusConfig.DefaultConnectionFactory;

            //while(retry_count > 0)
            while(true)
            using (var connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection(cf.HostName.Split(',')))
            using (var channel = connection.CreateModel())
            {
                if (retry_count <= 0) throw new Exception("RetryLimitException");


                string replyQueueName = null;
                QueueingBasicConsumer consumer = null;


                    try
                    {
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
                    }
                    catch (TopologyRecoveryException ex)
                    {
                        // connection fail.
                        _logger.Warn("Retry (left: {0}) ...", retry_count);
                        retry_count--;
                        Task.Delay(retryWait).Wait();
                        continue;
                    }

                    if (reply)
                {
                    replyQueueName = channel.QueueDeclare().QueueName;
                    consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(
                        queue: replyQueueName,
                        noAck: true,
                        consumer: consumer);
                }


                try
                {
                    IBasicProperties props = channel.CreateBasicProperties();
                    props.ContentType = "application/json";
                    props.Expiration = messageExpirationTimeout.TotalMilliseconds.ToString();

                    if (reply)
                    {
                        message.correlationId =
                        props.CorrelationId = Guid.NewGuid().ToString("N");
                        props.ReplyTo = replyQueueName;
                    }

                    if (this.BusType == MessageBusTypeEnum.EXCHANGE)
                    {
                        channel.BasicPublish(
                            exchange: this.ExchangeName ?? "",
                            routingKey: routing,
                            basicProperties: props,
                            body: Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message)));
                    }
                    else if (this.BusType == MessageBusTypeEnum.QUEUE)
                    {
                        channel.BasicPublish(
                            exchange: "",
                            routingKey: this.QueueName,
                            basicProperties: props,
                            body: Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message)));
                    }


                    if (reply)
                    {
                        BasicDeliverEventArgs ea;
                        if (consumer.Queue.Dequeue((int)waitReplyTimeout.TotalMilliseconds, out ea))
                        {
                            // done, receive response message
                            TOutputMessage response = JsonConvert.DeserializeObject<TOutputMessage>(Encoding.Unicode.GetString(ea.Body));

                            //if (string.IsNullOrEmpty(response.exception) == false)
                            //{
                            //    throw new Exception("RPC Exception: " + response.exception);
                            //}
                            return response;
                        }
                        else
                        {
                            // timeout, do not wait anymore
                            throw new TimeoutException(string.Format(
                                "MessageBus 沒有在訊息指定的時間內 ({0}) 收到 ResponseMessage 回覆。",
                                messageExpirationTimeout));
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

            return null;
        }

        public void Dispose()
        {

        }
    }

}
