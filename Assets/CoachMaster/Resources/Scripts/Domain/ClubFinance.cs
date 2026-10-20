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
    }
}
