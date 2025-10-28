namespace EcoWrap.Models.Response
{
    public class SellerResponse
    {
        public int SellerId { get; set; }
        public int UserId { get; set; }
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? AccountHolderName { get; set; }
        public string? BankBranch { get; set; }
        public string? QrcodeImage { get; set; }
        public string? CompanyImage { get; set; }
        public string? CompanyBanner { get; set; }
    }
}