using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VWParty.Infra.Messaging;

namespace Zeus.Messaging.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebQueue wq = new WebQueue("rpc_test"))
            {
                ////DIRECT=TCP:157.34.104.22\MyQueue
                //ConfigurationManager.ConnectionStrings["queues.data"].ConnectionString,
                //ConfigurationManager.ConnectionStrings["queues.ack"].ConnectionString,
                //ConfigurationManager.ConnectionStrings["queues.return"].ConnectionString);

                Random rnd = new Random();

                Stopwatch timer = new Stopwatch();
                while (true)
                {
                    //byte[] buffer = new byte[1024]; //new byte[4 * 1024 * 1024 - 5120];
                    //rnd.NextBytes(buffer);

                    RequestMessage request = new RequestMessage()
                    {
                        input_json = Guid.NewGuid().ToString()
                        //Convert.ToBase64String(buffer)
                    };

                    


                    try
                    {
                        Console.Write("* Call Remote ({0})...", request.input_json);
                        ResponseMessage response = wq.CallWorkerProcess(request);
                        Console.WriteLine(", Receive Response: {0}", response.result_json);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(", Exception: {0}", ex);
                    }

                    //string id = wq.SendMessage(request, TimeSpan.FromMinutes(60));
                    //Console.WriteLine("send: {0}... ", id); //request.input_json));


                    //TimeSpan wait_timeout = TimeSpan.FromSeconds(60);


                    //Console.Write("WAIT RETURN: ");
                    //int is_return = 0;
                    //timer.Restart();
                    //while(is_return != 2 && timer.Elapsed < wait_timeout)
                    //{
                    //    is_return = wq.IsReturned(id);
                    //    Task.Delay(100).Wait();
                    //    Console.Write(is_return);
                    //}

                    //if (is_return == 2)
                    //{
                    //    ResponseMessage response = wq.ReceiveResponse(id, TimeSpan.FromMinutes(30));
                    //    Console.WriteLine(" > RETURNED");
                    //    Console.WriteLine(string.Format("receive: {0}, execute time: {1} msec.", id, timer.ElapsedMilliseconds));
                    //}
                    //else
                    //{
                    //    Console.WriteLine("TIMEOUT");
                    //}
                }
            }
        }
    }
}
