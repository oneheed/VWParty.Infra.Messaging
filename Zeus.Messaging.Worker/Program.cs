using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VWParty.Infra.LogTracking;
using VWParty.Infra.Messaging.Core;
using VWParty.Infra.Messaging.RPCWorkers;

namespace Zeus.Messaging.Worker
{
    class Program
    {
        static POCWorker poc = null;

        // usage: Worker [worker count]
        static void Main(string[] args)
        {
            Console.WriteLine("Press [ENTER] to exit...");

            using (poc = new POCWorker())
            {
                var x = poc.StartWorkersAsync(10);

                int period = 1000 * 5; // 每 5 sec 偵測一次 ENTER，同時統計已處理的 message 數量
                while(Console.KeyAvailable == false && x.Wait(period) == false)
                {
                    Console.WriteLine("total call in current period: {0}, process speed: {1:0.00} call/sec", poc.count, poc.count * 1000.0 / period);
                    poc.count = 0;
                }

                poc.StopWorkers();
                Console.WriteLine("shutdown worker...");
                x.Wait();
                Console.WriteLine("shutdown worker complete.");
            }
        }
        
    }

}
