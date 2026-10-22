using System;
using System.Collections.Generic;

namespace CoachMaster.Domain
{
    public sealed class ClubFinance
    {
        public int Balance { get; set; }
        public int WageBudget { get; set; }
        public int TransferBudget { get; set; }
        public int TransferSpentThisSeason { get; set; }
        public int TicketIncomeThisSeason { get; set; }
        public int ShirtIncomeThisSeason { get; set; }
        public int PlayerSalesThisSeason { get; set; }
        public string PublicSituation { get; set; }
        public List<FinanceTransaction> Transactions { get; } = new List<FinanceTransaction>();
    }

    public sealed class FinanceTransaction
    {
        public long DateTicks { get; set; }
        public int Amount { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string PlayerId { get; set; }
    }
}
