using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;

namespace VWParty.Infra.Messaging.Core
{
    internal class MessagePack
    {
        public TimerMessage _message;
        public LogTrackerContext _context;
    }

    public class TimerServer : MessageServerBase<TimerMessage>
    {
        public TimerServer() : base("scheduler")
        {

        }

        private List<MessagePack> schedule = new List<MessagePack>();

        public AutoResetEvent outputWait = new AutoResetEvent(false);

        protected override void ExecuteSubscriberProcess(TimerMessage message, LogTrackerContext logtracker)
        {
            lock (schedule)
            {
                this.schedule.Add(new MessagePack()
                {
                    _message = message,
                    _context = logtracker
                });
                this.schedule.Sort((t1, t2) => { return DateTime.Compare(t1._message.RunAt, t2._message.RunAt); });
            }
            this.outputWait.Set();
        }






        private bool _stop_forwarder = false;
        public void StopForwarder()
        {
            _stop_forwarder = true;
            this.outputWait.Set();
        }

        public void ForwardMessage()
        {
            _stop_forwarder = false;
            while (_stop_forwarder == false)
            {
                while (this.schedule.Count > 0)
                {
                    MessagePack msgpack = this.schedule[0];
                    TimerMessage msg = msgpack._message;
                    LogTrackerContext ctx = msgpack._context;

                    msg.RunAt = msg.RunAt.ToUniversalTime();

                    MessageClientBase<InputMessageBase> mcb = null;

                    if (string.IsNullOrEmpty(msg.QueueName))
                    {
                        mcb = new MessageClientBase<InputMessageBase>(msg.ExchangeName, msg.ExchangeType);
                    }
                    else
                    {
                        mcb = new MessageClientBase<InputMessageBase>(msg.QueueName);
                    }


                    if (msg.RunAt <= DateTime.UtcNow)
                    {
                        // do expired
                        Console.WriteLine("Run Expired Task: {0}, {1}, {2}", msg.ID, msg.RouteKey, msg.RunAt);

                        lock (schedule) schedule.Remove(msgpack);
                        mcb.PublishMessageAsync(
                            msg.RouteKey, 
                            Encoding.Unicode.GetBytes(msg.MessageText),
                            ctx).Wait();
                    }
                    else if (outputWait.WaitOne(msg.RunAt - DateTime.UtcNow) == false)
                    {
                        // do now
                        Console.WriteLine("Run OnTime Task: {0}, {1}, {2}", msg.ID, msg.RouteKey, msg.RunAt);

                        lock (schedule) schedule.Remove(msgpack);
                        //stp.PublishMessage(msg.RouteKey, msg);
                        mcb.PublishMessageAsync(
                            msg.RouteKey,
                            Encoding.Unicode.GetBytes(msg.MessageText),
                            ctx).Wait();
                    }
                    else
                    {
                        Console.WriteLine("Next Job:");
                        lock (schedule) foreach (MessagePack m in schedule)
                        {
                            Console.WriteLine("- [{0}][{1}], {2}", m._message.ID, m._message.RouteKey, m._message.RunAt);
                        }
                    }

                    mcb.Dispose();

                    if (this.Status == WorkerStatusEnum.STOPPED)
                    {
                        // do shutdown work
                        return;
                    }
                }

                this.outputWait.WaitOne();
            }
        }
    }
}
