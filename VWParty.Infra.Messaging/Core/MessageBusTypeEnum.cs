﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging.Core
{
    public enum MessageBusTypeEnum
    {
        EXCHANGE = 1,
        QUEUE = 2,
        NULL = 0
    }
}
