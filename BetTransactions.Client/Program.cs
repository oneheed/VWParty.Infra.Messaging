using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.Messaging.BetTransactions;

namespace BetTransactions.Client
{
    class Program
    {
        static Logger _logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
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
                Task.Delay(300).Wait();
                bus.PublishMessage("letou", new BetTransactionMessage()
                {
                    Id = string.Format("{0}-{1}", i, Guid.NewGuid())
                });
                Console.Write('.');

                if (i % 100 == 0)
                {
                    Console.WriteLine("Message Published: {0}, {1:0.00} msg/sec", i, 100 * 1000 / timer.ElapsedMilliseconds);
                    timer.Restart();
                }
            }
        }
    }
}
