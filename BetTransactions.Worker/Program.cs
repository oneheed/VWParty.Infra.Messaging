using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;
using VWParty.Infra.Messaging.BetTransactions;
using VWParty.Infra.Messaging.Core;

namespace BetTransactions.Worker
{
    class Program
    {
        static Logger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            using (BetRpcServer brs = new BetRpcServer("bet_test"))
            {
                var x = brs.StartWorkersAsync(10);

                Console.WriteLine("PRESS [ENTER] To Exit...");
                Console.ReadLine();

                brs.StopWorkers();
                x.Wait();

                Console.WriteLine("Shutdown complete.");
            }
        }

/*
        static void _Main(string[] args)
        {

            using (BetMessageSubscriber bus = new BetMessageSubscriber("bet_test"))
            {
                var result = bus.StartWorkersAsync((bm, tracker) =>
                {
                    LogTrackerContext.Init(LogTrackerContextStorageTypeEnum.THREAD_DATASLOT, tracker);
                    //Console.WriteLine("[{0:00}] {1} ...", Thread.CurrentThread.ManagedThreadId, bm.Id);
                    _logger.Info(bm.Id);
                    return new VWParty.Infra.Messaging.Core.OutputMessageBase();
                }, 10);

                Console.WriteLine("PRESS [ENTER] To Exit...");
                Console.ReadLine();


                Console.WriteLine("Shutdown worker...");
                _logger.Info("Shutdown worker...");

                bus.StopWorkers();
                result.Wait();

                Console.WriteLine("Shutdown complete.");
                _logger.Info("Shutdown complete.");

            }
            
        }
*/
    }

    public class BetRpcServer: RpcServerBase<BetTransactionMessage, OutputMessageBase>
    {
        public BetRpcServer(string queueName) : base(queueName)
        {

        }

        Logger _logger = LogManager.GetCurrentClassLogger();

        protected override OutputMessageBase ExecuteSubscriberProcess(BetTransactionMessage message, LogTrackerContext logtracker)
        {
            LogTrackerContext.Init(LogTrackerContextStorageTypeEnum.THREAD_DATASLOT, logtracker);
            //Console.WriteLine("[{0:00}] {1} ...", Thread.CurrentThread.ManagedThreadId, bm.Id);
            _logger.Info(message.Id);
            return new VWParty.Infra.Messaging.Core.OutputMessageBase();
        }
    }
}
