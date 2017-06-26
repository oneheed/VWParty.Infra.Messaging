using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        static BetRpcServer brs = null;
        static AutoResetEvent w = new AutoResetEvent(false);
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        //static EventHandler _handler;
        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        private static bool ShutdownHandler(CtrlType sig)
        {
            _logger.Info("Shutdown Console Apps...");
            brs.StopWorkers();
            w.Set();
            _logger.Info("Shutdown Console Apps2...");
            return true;
        }






        static Logger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {

            //using (BetRpcServer brs = new BetRpcServer())
            using ( brs = new BetRpcServer())
            {
                var x = brs.StartWorkersAsync(10);
                SetConsoleCtrlHandler(ShutdownHandler, true);

                //Console.WriteLine("PRESS [ENTER] To Exit...");
                //Console.ReadLine();
                w.WaitOne();

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
        public BetRpcServer() : base("bet_test")
        {

        }

        Logger _logger = LogManager.GetCurrentClassLogger();

        protected override OutputMessageBase ExecuteSubscriberProcess(BetTransactionMessage message, LogTrackerContext logtracker)
        {
            LogTrackerContext.Init(LogTrackerContextStorageTypeEnum.THREAD_DATASLOT, logtracker);
            _logger.Info(message.Id);
            return new VWParty.Infra.Messaging.Core.OutputMessageBase();
        }
    }

    public class BetMessageServer: MessageServerBase<BetTransactionMessage>
    {
        public BetMessageServer() : base("bet_test")
        {

        }
        Logger _logger = LogManager.GetCurrentClassLogger();
        protected override void ExecuteSubscriberProcess(BetTransactionMessage message, LogTrackerContext logtracker)
        {
            LogTrackerContext.Init(LogTrackerContextStorageTypeEnum.THREAD_DATASLOT, logtracker);
            _logger.Info(message.Id);
            //Console.WriteLine(message.Id);
            return;
        }
    }
}
