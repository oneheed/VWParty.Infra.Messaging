using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Messaging
{
    internal class QueueConfig
    {
        public static readonly IMessageFormatter MessageFormatter =
            //new BinaryMessageFormatter();
            //new JsonMessageFormatter();
            new XmlMessageFormatter(
                new Type[] {
                typeof(RequestMessage),
                typeof(ResponseMessage) });

        public static MessagePropertyFilter MessageFilter = null;

        static QueueConfig()
        {
            MessageFilter = new MessagePropertyFilter();
            MessageFilter.SetAll();
            MessageFilter.DefaultBodySize = 1024 * 1024;
        }
    }
}
