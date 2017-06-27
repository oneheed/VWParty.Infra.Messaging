using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;
using VWParty.Infra.Messaging;
using VWParty.Infra.Messaging.BetTransactions;
using VWParty.Infra.Messaging.Core;

namespace BetTransactions.Client
{
    class Program
    {
        static Logger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            string mode = (args.Length == 0) ? "ASYNC" : args[0].ToUpper();
            Console.WriteLine("Start in {0} mode.", mode);

            BetMessageClient bmc = null;
            BetRpcClient brc = null;
            BetTimerClient btc = null;

            LogTrackerContext.Create("POC/BetTransactions.Client", LogTrackerContextStorageTypeEnum.THREAD_DATASLOT);
            _logger.Info("Init...");

            switch (mode)
            {
                case "ASYNC":
                    bmc = new BetMessageClient();
                    break;

                case "SYNC":
                    brc = new BetRpcClient();
                    break;

                case "TIMER":
                    btc = new BetTimerClient();
                    break;
            }


            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 1; i <= 1000000; i++)
            {
                BetTransactionMessage btm = new BetTransactionMessage()
                {
                    Id = string.Format("{0}-{1}", i, Guid.NewGuid())
                };

                switch (mode)
                {
                    case "ASYNC":
                        bmc.PublishAsync("letou", btm).Wait();
                        break;

                    case "SYNC":
                        try
                        {
                            OutputMessageBase omb = brc.CallAsync("letou", btm).Result;
                        }
                        catch(AggregateException ae)
                        {
                            foreach(Exception ex in ae.InnerExceptions)
                                Console.WriteLine(ex);
                        }
                        break;

                    case "TIMER":
                        btc.PublishAsync("letou", btm, TimeSpan.FromSeconds(50)).Wait();
                        break;
                }
                

                if (args.Length > 1 && args[1] == "delay") Task.Delay(300).Wait();
                Console.Write('.');

                if (i % 100 == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Message Published: {0}, {1:0.00} msg/sec", i, 100 * 1000 / timer.ElapsedMilliseconds);
                    timer.Restart();
                }
            }
        }

        /*
        static void _Main(string[] args)
        {
            //BetMessageBus bus = new BetMessageBus();
            BetMessagePublisher bus = new BetMessagePublisher()
            {
                IsWaitReturn = (args.Length > 0 && args[0] == "sync")
            };

            Console.WriteLine("Start BetMessagePublisher in {0} mode.", bus.IsWaitReturn ? "SYNC" : "ASYNC");

            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 1; i <= 1000000; i++)
            {
                //Task.Delay(300).Wait();
                bus.PublishMessageAsync("letou", new BetTransactionMessage()
                {
                    Id = string.Format("{0}-{1}", i, Guid.NewGuid())
                }).Wait();
                Console.Write('.');

                if (i % 100 == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Message Published: {0}, {1:0.00} msg/sec", i, 100 * 1000 / timer.ElapsedMilliseconds);
                    timer.Restart();
                }
            }
        }
        */
    }





    public class BetMessageClient : MessageClientBase<BetTransactionMessage>
    {
        public BetMessageClient() : base("tp-transaction", "direct")
        {

        }

        //public LogTrackerContext Tracker = null;
        //public override Task PublishAsync(string routing, BetTransactionMessage message)
        //{
        //    return this.PublishMessageAsync(
        //        this.IsWaitReturn,
        //        MessageBusConfig.DefaultWaitReplyTimeOut,
        //        MessageBusConfig.DefaultMessageExpirationTimeout,
        //        routing,
        //        message,
        //        MessageBusConfig.DefaultRetryCount,
        //        MessageBusConfig.DefaultRetryWaitTime,
        //        this.Tracker);
        //}
    }

    public class BetRpcClient : RpcClientBase<BetTransactionMessage, OutputMessageBase>
    {
        public BetRpcClient() : base("tp-transaction", "direct")
        {

        }
    }

    public class BetTimerClient : TimerClient<BetTransactionMessage>
    {
        public BetTimerClient() : base("tp-transaction", "direct")
        {

        }
    }
}
