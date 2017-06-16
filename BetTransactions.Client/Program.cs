using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.Messaging.BetTransactions;
using VWParty.Infra.Messaging.Core;

namespace BetTransactions.Client
{
    class Program
    {
        static Logger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            bool IsWaitReturn = (args.Length > 0 && args[0] == "sync");
            Console.WriteLine("Start in {0} mode.", IsWaitReturn ? "SYNC" : "ASYNC");

            BetMessageClient bmc = null;
            BetRpcClient brc = null;
            if (IsWaitReturn == true)
            {
                // sync mode
                brc = new BetRpcClient();
            }
            else
            {
                // async mode
                bmc = new BetMessageClient();
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 1; i <= 1000000; i++)
            {
                BetTransactionMessage btm = new BetTransactionMessage()
                {
                    Id = string.Format("{0}-{1}", i, Guid.NewGuid())
                };

                if (IsWaitReturn)
                {
                    OutputMessageBase omb = brc.CallAsync("letou", btm).Result;
                }
                else
                {
                    bmc.PublishAsync("letou", btm).Wait();
                }

                //Task.Delay(300).Wait();
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
    }

    public class BetRpcClient : RpcClientBase<BetTransactionMessage, OutputMessageBase>
    {
        public BetRpcClient() : base("tp-transaction", "direct")
        {

        }
    }
}
