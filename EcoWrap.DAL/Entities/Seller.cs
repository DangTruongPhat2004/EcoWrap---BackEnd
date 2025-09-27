using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Seller")]
public partial class Seller
{
    [Key]
    [Column("Seller_id")]
    public int SellerId { get; set; }

    [Column("User_id")]
    public int UserId { get; set; }

    [Column("Company_name")]
    [StringLength(255)]
    public string? CompanyName { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [InverseProperty("Seller")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    [ForeignKey("UserId")]
    [InverseProperty("Sellers")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Seller")]
    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
