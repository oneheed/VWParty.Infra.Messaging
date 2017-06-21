using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.Messaging.Core;

namespace VWParty.Infra.Messaging.TimerWorker
{
    class Program
    {
        static void Main(string[] args)
        {
            using (TimerServer ts = new TimerServer())
            {

                var worker = ts.StartWorkersAsync();

                while (true) Task.Delay(100).Wait();    // infinity loop, 在 windows container 的 daemon mode 下, ReadLine() 會讀取 STDIN 失敗

                ts.StopWorkers();
                Console.WriteLine("stop workers...");

                worker.Wait();
                Console.WriteLine("worker stopped.");
            }
        }
    }
}
