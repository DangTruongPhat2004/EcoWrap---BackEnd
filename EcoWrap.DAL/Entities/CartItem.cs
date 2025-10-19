using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("CartItem")]
public class CartItem
{
    [Key]
    [Column("CartItem_id")]
    public int CartItemId { get; set; }

    [Column("Cart_id")]
    public int CartId { get; set; }

    [Column("Product_id")]
    [StringLength(15)]
    [Unicode(false)]
    public string ProductId { get; set; } = null!;

    [Column("Seller_id")]
    public int SellerId { get; set; }

    [Column("Quantity")]
    public int Quantity { get; set; }

    [Column("Cart_item_price", TypeName = "decimal(18,2)")]
    public decimal CartItemPrice { get; set; }

    [Column("Design_file")]
    public string? DesignFile { get; set; }

    [Column("Order_id")]
    public int? OrderId { get; set; }

    [ForeignKey("CartId")]
    [InverseProperty("CartItems")]
    public virtual Cart Cart { get; set; } = null!;

    [ForeignKey("OrderId")]
    [InverseProperty("CartItems")]
    public virtual Order? Order { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("CartItems")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("SellerId")]
    [InverseProperty("CartItems")]
    public virtual Seller Seller { get; set; } = null!;
}