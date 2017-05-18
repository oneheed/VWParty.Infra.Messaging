using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging.BetTransactions
{
    public enum BetTransactionTypeEnum : int
    {
        BET = 1,            // 下注
        WIN = 2,            // 派彩
        Refund = 3,         // 取消下注
        Bonus = 4,          // 獎勵
        NegativeBet = 5,    // 
        Unknown = 99,       // 未知的型別
    }
}
