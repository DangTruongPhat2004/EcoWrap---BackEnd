namespace EcoWrap.Models.Response
{
    public class DashboardResponse
    {
        public int TotalVerifiedUsers { get; set; }
        public int TotalCustomer { get; set; }
        public int TotalSeller { get; set; }
        public int TotalFeedbacks { get; set; }
        public double AverageRating { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalSuccessfulTransactions { get; set; }
    }
}