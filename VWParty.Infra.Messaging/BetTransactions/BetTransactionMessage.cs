using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VWParty.Infra.Messaging.BetTransactions
{
    public class BetTransactionMessage
    {
        // key fields
        public string Id { get; set; }
        public string BrandId { get; set; }
        public string PlatformCode { get; set; }
        public string GameId { get; set; }
        public string AccountId { get; set; }
        public string BetId { get; set; }
        public string TransactionNumber { get; set; }
        public string RoundId { get; set; }

        // content fields
        public decimal TransactionAmoung { get; set; }
        public DateTime TransactionDateTime { get; set; }
        public BetTransactionTypeEnum TransactionType { get; set; }
        public bool IsTransactionFinished { get; set; }


        // property fields
        public string RawRequestJson { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
