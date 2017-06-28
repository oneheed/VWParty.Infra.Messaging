using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;
using VWParty.Infra.Messaging.Core;

namespace VWParty.Infra.Messaging.RPCWorkers
{
    public class WebQueue : RpcClientBase<RequestMessage, ResponseMessage> //MessagePublisherBase<RequestMessage, ResponseMessage>
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        //private string QueueName = null;

        public WebQueue(string queueName): 
            base(queueName)
            
        {
            //this.QueueName = queueName;
            //_logger.Info("create webqueue connection - host: {0}, port: {1}", factory.HostName, factory.Port);
        }

        //public ResponseMessage CallWorkerProcess(RequestMessage request)
        //{
        //    return this.CallWorkerProcess(request, TimeSpan.FromSeconds(10));
        //}
        //public ResponseMessage CallWorkerProcess(RequestMessage request, TimeSpan messageExpirationTimeout)
        //{
        //    return this.PublishMessageAsync(
        //        true,
        //        TimeSpan.FromSeconds(10),
        //        messageExpirationTimeout,
        //        this.QueueName,
        //        request,
        //        MessageBusConfig.DefaultRetryCount,
        //        MessageBusConfig.DefaultRetryWaitTime,
        //        LogTrackerContext.Current).Result;
        //}

        [Obsolete("向前相容用，請改用 Call() or CallAsync() 代替", true)]
        public ResponseMessage CallWorkerProcess(RequestMessage request)
        {
            return this.CallAsync(null, request).Result;
        }

    }
    
}
