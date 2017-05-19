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
        static void Main(string[] args)
        {
            BetMessageBus bus = new BetMessageBus();

            bus.StartSubscribeWorker(
                "bet_test",
                false,
                (bm) =>
                {
                    Console.WriteLine("[{0}] {1} ...", Thread.CurrentThread.ManagedThreadId, bm.Id);
                    return null;
                },
                50);

            Console.WriteLine("PRESS [ENTER] To Exit...");
            Console.ReadLine();

            Console.WriteLine("Shutdown worker...");
            bus.Stop();
            Console.WriteLine("Shutdown complete.");
        }
    }
}
