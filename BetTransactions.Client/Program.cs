using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.Messaging.BetTransactions;

namespace BetTransactions.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            BetMessageBus bus = new BetMessageBus();

            for (int i = 0; i < 1000000; i++)
            {
                bus.PublishMessage("letou", new BetTransactionMessage()
                {
                    Id = string.Format("{0}-{1}", i, Guid.NewGuid())
                });
                if (i % 100 == 0) Console.WriteLine("Message Published: {0}...", i);
            }
        }
    }
}
