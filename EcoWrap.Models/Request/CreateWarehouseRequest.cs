using System.ComponentModel.DataAnnotations;

namespace EcoWrap.Models.Request
{
    public class CreateWarehouseRequest
    {
        public string WarehouseName { get; set; } = null!;
        public string Address { get; set; } = null!;
    }
}