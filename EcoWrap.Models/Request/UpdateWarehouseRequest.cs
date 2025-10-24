using System.ComponentModel.DataAnnotations;

namespace EcoWrap.Models.Request;

public class UpdateWarehouseRequest
{
    [Required]
    [StringLength(255)]
    public string WarehouseName { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string Address { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string ProvinceId { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string DistrictId { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string WardId { get; set; } = null!; 
}