using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Messaging
{
    public class WebQueue
    {
        //private static readonly TimeSpan TimeToBeReceived = TimeSpan.FromMinutes(15.0);
        private string[] names = new string[]
        {
            "data",
            "ack",
            "return"
        };

        private Dictionary<string, MessageQueue> queues = new Dictionary<string, MessageQueue>();

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public WebQueue(string dataQueuePath, string ackQueuePath, string returnQueuePath)
        {
            this.queues["data"] = new MessageQueue(dataQueuePath);
            this.queues["ack"] = new MessageQueue(ackQueuePath);
            this.queues["return"] = new MessageQueue(returnQueuePath);

            foreach (string name in this.names)
            {
                this.queues[name].Formatter = QueueConfig.MessageFormatter;
                this.queues[name].MessageReadPropertyFilter = QueueConfig.MessageFilter;
            }
        }

        #region for client API(s)

        public ResponseMessage CallWorkerProcess(RequestMessage request)
        {
            string msgid = this.SendMessage(request, TimeSpan.FromMinutes(5.0));
            //this.ReceiveAcknowledge(msgid, TimeSpan.FromMinutes(30.0));
            ResponseMessage response = this.ReceiveResponse(msgid, TimeSpan.FromMinutes(30.0));

            if (string.IsNullOrEmpty(response.exception) == false)
            {
                throw new Exception("remote call exception: " + response.exception);
            }

            return response;
        }

        public string SendMessage(RequestMessage request, TimeSpan maxTimeToBeReceived)
        {
            try
            {
                Message myMessage = new Message();
                myMessage.Formatter = QueueConfig.MessageFormatter; //this.queues["data"].Formatter;
                myMessage.Body = request;

                //myMessage.UseJournalQueue = true;

                //myMessage.AcknowledgeType = AcknowledgeTypes.PositiveReceive;// | AcknowledgeTypes.PositiveArrival;
                myMessage.AcknowledgeType = AcknowledgeTypes.FullReachQueue | AcknowledgeTypes.FullReceive;
                myMessage.AdministrationQueue = this.queues["ack"];

                myMessage.ResponseQueue = this.queues["return"];

                myMessage.TimeToBeReceived = maxTimeToBeReceived;
                this.queues["data"].Send(myMessage, MessageQueueTransactionType.None);

                request.CorrelationId = myMessage.Id;
                return myMessage.Id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }

        }

        public Message ReceiveAcknowledge(string correlationID, TimeSpan waitTimeout)
        {
            return this.queues["ack"].ReceiveByCorrelationId(correlationID, waitTimeout);
        }

        public ResponseMessage ReceiveResponse(string correlationID, TimeSpan waitTimeout)
        {
            this.queues["return"].MessageReadPropertyFilter = QueueConfig.MessageFilter;
            return this.queues["return"].ReceiveByCorrelationId(correlationID, waitTimeout).Body as ResponseMessage;
        }





        public int IsReturned(string correlationID)
        {
            int status = 0;
            try
            {
                var ackmsg = this.queues["ack"].PeekByCorrelationId(correlationID);
                status = 1;

                //Console.Write("[{0}]", ackmsg.Acknowledgment);

                this.queues["return"].PeekByCorrelationId(correlationID);
                status = 2;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex);
            }
            return status;
        }
        #endregion



    }




}
