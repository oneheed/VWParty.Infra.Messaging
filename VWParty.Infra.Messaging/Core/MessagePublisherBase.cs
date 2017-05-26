using Newtonsoft.Json;
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
                false,
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
            using (var connection = MessageBusConfig.DefaultConnectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                string replyQueueName = null;
                QueueingBasicConsumer consumer = null;

                if (this.BusType == MessageBusTypeEnum.QUEUE)
                {
                    channel.QueueDeclare(
                        queue: routing,
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

                    channel.BasicPublish(
                        exchange: this.ExchangeName ?? "",
                        routingKey: routing,
                        basicProperties: props,
                        body: Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message)));


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
