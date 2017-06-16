using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VWParty.Infra.Messaging.Core;

namespace VWParty.Infra.Messaging.RPCWorkers
{
    public class WorkerQueue : RpcServerBase<RequestMessage, ResponseMessage> //MessageSubscriberBase<RequestMessage, ResponseMessage>
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
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

        public WorkerQueue(string queueName) :
            base(queueName)
        {
        }

        //public delegate TOutputMessage SubscriberProcess(TInputMessage message);

        [Obsolete("只為了向前相容而設計的型別，請改用 SubscriberProcess", true)]
        public delegate ResponseMessage WorkerProcess(RequestMessage message);

        [Obsolete("只為了向前相容而設計的wrapper，請改用 StartWorkersAsync(), 並 override ProcessMessage 填入處理 message 的動作", true)]
        public void StartWorkers(WorkerProcess process, int worker_count)
        {
            SubscriberProcess x = (req, tracker) => {
                this.CurrentZeusRequestId = req.request_id;
                return process(req);
            };
            this.StartWorkersAsync(
                (SubscriberProcess)((req, tracker) => 
                {
                    this.CurrentZeusRequestId = req.request_id;
                    return process(req);
                }), 
                worker_count).Wait();
        }
    }



}

