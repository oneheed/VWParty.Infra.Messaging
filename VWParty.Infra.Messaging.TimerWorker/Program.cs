using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
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

        static void Main(string[] args)
        {
            using (ts = new TimerServer())
            {
                Console.CancelKeyPress += Console_CancelKeyPress;
                var worker = ts.StartWorkersAsync();

                //while (true) Task.Delay(100).Wait();    // infinity loop, 在 windows container 的 daemon mode 下, ReadLine() 會讀取 STDIN 失敗
                w.WaitOne();
                //ts.StopWorkers();

                //Console.WriteLine("stop workers...");
                _logger.Info("stop workers...");

                worker.Wait();
                //Console.WriteLine("worker stopped.");
                _logger.Info("worker stopped.");
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //Console.WriteLine("Cancel Pressed...");
            _logger.Info("Key pressed: [CTRL-C]");
            ts.StopWorkers();
            w.Set();
        }
    }
}
