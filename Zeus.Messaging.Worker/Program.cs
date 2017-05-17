using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Messaging.Worker
{
    class Program
    {
        // usage: Worker [worker count]
        static void Main(string[] args)
        {
            //DIRECT=TCP:157.34.104.22\MyQueue
            using (WorkerQueue wq = new WorkerQueue("rpc_test"))
            {
                // ConfigurationManager.ConnectionStrings["queues.data"].ConnectionString);
                int count = 0;

                WorkerQueue.WorkerProcess proc = (request) =>
                {
                    Interlocked.Increment(ref count);
                    Console.Write("-");
                    return new ResponseMessage()
                    {
                        result_json = request.input_json
                    };
                };

                //int worker_count = 1;
                //if (args.Length > 0) worker_count = int.Parse(args[0]);
                //for(int i = 0; i < worker_count; i++) wq.StartAsync(proc);


                bool stop = false;
                Task statistic_task = Task.Factory.StartNew(() =>
                {
                    TimeSpan period = TimeSpan.FromSeconds(10);
                    while (stop == false)
                    {
                        count = 0;

                        Task.Delay(period).Wait();
                        Console.WriteLine();
                    //Console.WriteLine($"處理速度: {count / period.TotalSeconds} requests/sec");
                }
                });

                wq.StartWorkers(proc, 1);

                Console.WriteLine("Press [ENTER] to quit...");
                Console.ReadLine();

                Console.Write("Shutdown Worker...");
                wq.Stop();
                Console.WriteLine("OK (worker)");

                Console.Write("Shutdown Statistic Task...");
                stop = true;
                statistic_task.Wait();
                Console.WriteLine("OK (statistic)");
            }
        }
    }
}
