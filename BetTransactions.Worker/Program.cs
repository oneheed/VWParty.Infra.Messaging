﻿using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VWParty.Infra.Messaging.BetTransactions;

namespace BetTransactions.Worker
{
    class Program
    {
        static Logger _logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {

            using (BetMessageSubscriber bus = new BetMessageSubscriber("bet_test"))
            {
                var result = bus.StartWorkersAsync((bm) =>
                {
                    //Console.WriteLine("[{0:00}] {1} ...", Thread.CurrentThread.ManagedThreadId, bm.Id);
                    _logger.Info(bm.Id);
                    return new VWParty.Infra.Messaging.Core.OutputMessageBase();
                }, 10);

                Console.WriteLine("PRESS [ENTER] To Exit...");
                Console.ReadLine();


                Console.WriteLine("Shutdown worker...");
                _logger.Info("Shutdown worker...");

                bus.StopWorker();
                result.Wait();

                Console.WriteLine("Shutdown complete.");
                _logger.Info("Shutdown complete.");

            }
            
        }
    }
}
