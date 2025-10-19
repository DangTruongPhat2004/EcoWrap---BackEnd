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

    [StringLength(50)]
    public string? BankAccountNumber { get; set; }

    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(100)]
    public string? AccountHolderName { get; set; }

    [StringLength(100)]
    public string? BankBranch { get; set; }

    [Column("QRCodeImage")]
    public string? QrcodeImage { get; set; }

    [InverseProperty("Seller")]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [InverseProperty("Seller")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("Seller")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    [ForeignKey("UserId")]
    [InverseProperty("Sellers")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Seller")]
    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
