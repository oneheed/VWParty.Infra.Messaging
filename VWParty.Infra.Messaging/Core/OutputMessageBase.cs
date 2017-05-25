using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging.Core
{
    public class OutputMessageBase
    {
        public string correlationId { get; set; }
        public string exception { get; set; }
    }
}
