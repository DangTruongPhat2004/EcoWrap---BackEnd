namespace EcoWrap.Models.Request
{
    public class CreatInventoryRequest
    {
        public string ProductId { get; set; } = null!;
        public int WarehouseId { get; set; }
        public int StockQuantity { get; set; }
    }
}