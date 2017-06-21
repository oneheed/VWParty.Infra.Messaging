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

                Console.WriteLine("Press [ENTER]...");
                Console.ReadLine();

                ts.StopWorkers();
                Console.WriteLine("stop workers...");

                worker.Wait();
                Console.WriteLine("worker stopped.");
            }
        }
    }
}
