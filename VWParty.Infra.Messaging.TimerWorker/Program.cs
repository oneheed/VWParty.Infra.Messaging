using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VWParty.Infra.Messaging.Core;

namespace VWParty.Infra.Messaging.TimerWorker
{
    class Program
    {
        static TimerServer ts = null;
        static AutoResetEvent w = new AutoResetEvent(false);

        static Logger _logger = LogManager.GetCurrentClassLogger();



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
            ts.StopWorkers();
            w.Set();
            _logger.Info("Shutdown Console Apps2...");
            return true;
        }

        static void Main(string[] args)
        {
            //Console.CancelKeyPress += Console_CancelKeyPress;

            //_handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(ShutdownHandler, true);

            //AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;

            using (ts = new TimerServer())
            {
                var worker = ts.StartWorkersAsync();

                try
                {
                    //while (true) Task.Delay(100).Wait();    // infinity loop, 在 windows container 的 daemon mode 下, ReadLine() 會讀取 STDIN 失敗
                    w.WaitOne();
                }
                finally
                {

                    //ts.StopWorkers();
                    _logger.Info("stop workers...");

                    worker.Wait();
                    _logger.Info("worker stopped.");
                }
            }
        }

        private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            _logger.Info("Application Exit...");
            ts.StopWorkers();
            w.Set();
        }

        //private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        //{
        //    //Console.WriteLine("Cancel Pressed...");
        //    _logger.Info("Key pressed: [CTRL-C]");
        //    ts.StopWorkers();
        //    w.Set();
        //}
    }
}
