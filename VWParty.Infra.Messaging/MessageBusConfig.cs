﻿using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
//using System.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging
{
    internal class MessageBusConfig
    {
        private static string DefaultName = "MessageBus";

        public static ConnectionFactory DefaultConnectionFactory
        {
            get
            {
                var fac = GetMessageBusConnectionFactoryFromConfig(DefaultName);
#if (DEBUG)
                // default value for DEBUG mode
                if (fac == null) return new ConnectionFactory()
                {
                    HostName = "172.19.3.143",
                    Port = 5672
                };
#endif

                return fac;
            }
        }

        public static ConnectionFactory GetMessageBusConnectionFactoryFromConfig(string configName)
        {
            var configItem = ConfigurationManager.ConnectionStrings[configName];
            if (configItem == null || configItem.ProviderName != "RabbitMQ")
            {
                return null;
            }

            string connstr = configItem.ConnectionString;
            ConnectionFactory fac = new ConnectionFactory();
            fac.Port = 5672; // set default port

            foreach (string segment in connstr.Split(';'))
            {
                string[] temp = segment.Split('=');
                if (temp.Length != 2) continue;

                string name = temp[0];
                string value = temp[1];

                if (string.IsNullOrEmpty(value)) continue;

                switch (name)
                {
                    case "server":
                        //QueueHostName = value;
                        fac.HostName = value;
                        break;

                    case "port":
                        //QueuePortNumber = int.Parse(value);
                        fac.Port = int.Parse(value);
                        break;

                    case "username":
                        fac.UserName = value;
                        break;

                    case "password":
                        fac.Password = value;
                        break;
                }
            }

            return fac;
        }



        //        public static string QueueHostName = null;//"172.19.3.143";
        //        public static int QueuePortNumber = 5672;






        //        static QueueConfig()
        //        {
        //            //MessageFilter = new MessagePropertyFilter();
        //            //MessageFilter.SetAll();
        //            //MessageFilter.DefaultBodySize = 1024 * 1024;





        //            if (ConfigurationManager.ConnectionStrings["MessageBus"] != null && ConfigurationManager.ConnectionStrings["MessageBus"].ProviderName == "RabbitMQ")
        //            {
        //                string connstr = ConfigurationManager.ConnectionStrings["MessageBus"].ConnectionString;

        //                foreach (string segment in connstr.Split(';'))
        //                {
        //                    string[] temp = segment.Split('=');
        //                    if (temp.Length != 2) continue;

        //                    string name = temp[0];
        //                    string value = temp[1];

        //                    switch(name)
        //                    {
        //                        case "server":
        //                            QueueHostName = value;
        //                            break;

        //                        case "port":
        //                            QueuePortNumber = int.Parse(value);
        //                            break;

        //                        case "username":
        //                        case "password":
        //                            break;
        //                    }
        //                }
        //            }





        //        }
    }
}