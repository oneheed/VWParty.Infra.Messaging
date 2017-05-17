using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
//using System.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Messaging
{
    internal class QueueConfig
    {
        //public static readonly IMessageFormatter MessageFormatter =
        //    //new BinaryMessageFormatter();
        //    //new JsonMessageFormatter();
        //    new XmlMessageFormatter(
        //        new Type[] {
        //        typeof(RequestMessage),
        //        typeof(ResponseMessage) });

        //public static MessagePropertyFilter MessageFilter = null;

        public static string QueueHostName = null;//"172.19.3.143";
        public static int QueuePortNumber = 5672;






        static QueueConfig()
        {
            //MessageFilter = new MessagePropertyFilter();
            //MessageFilter.SetAll();
            //MessageFilter.DefaultBodySize = 1024 * 1024;

#if (DEBUG)
            // default value for DEBUG mode
            //QueueHostName = "172.19.3.143";
            //QueuePortNumber = 5672;
#endif



            if (ConfigurationManager.ConnectionStrings["MessageBus"] != null && ConfigurationManager.ConnectionStrings["MessageBus"].ProviderName == "RabbitMQ")
            {
                string connstr = ConfigurationManager.ConnectionStrings["MessageBus"].ConnectionString;

                foreach (string segment in connstr.Split(';'))
                {
                    string[] temp = segment.Split('=');
                    if (temp.Length != 2) continue;

                    string name = temp[0];
                    string value = temp[1];
                    
                    switch(name)
                    {
                        case "server":
                            QueueHostName = value;
                            break;

                        case "port":
                            QueuePortNumber = int.Parse(value);
                            break;

                        case "username":
                        case "password":
                            break;
                    }
                }
            }





        }
    }
}
