using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

public partial class EcoWrapDbContext : DbContext
{
    public EcoWrapDbContext()
    {
    }

    public EcoWrapDbContext(DbContextOptions<EcoWrapDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CarbonFootprint> CarbonFootprints { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Collection> Collections { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Registration> Registrations { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Seller> Sellers { get; set; }

    public virtual DbSet<Shipping> Shippings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;uid=sa;pwd=123456;Database=EcoWrap;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarbonFootprint>(entity =>
        {
            entity.HasKey(e => e.CarbonId).HasName("PK__CarbonFo__7D72AC77D408BC31");

            entity.HasOne(d => d.Product).WithMany(p => p.CarbonFootprints)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarbonFootprint_Product");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.CarbonFootprints)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarbonFootprint_Warehouse");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__D6862FC1A5EA0FED");

            entity.HasOne(d => d.Customer).WithMany(p => p.Carts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cart_Customer");

            entity.HasOne(d => d.Product).WithMany(p => p.Carts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cart_Product");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__6DB28136723B9B0F");

            entity.Property(e => e.CategoryId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.CollectionId).HasName("PK__Collecti__511BE6435FFBAB92");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Collections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Collections_Customer");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__8CB382B15EB1AFBB");

            entity.HasOne(d => d.User).WithMany(p => p.Customers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customer_Users");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__2B66F8038522233D");

            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Product");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Inventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Warehouse");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__F1FF845368BC46FF");

            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Customer");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__41E0DCE64127694C");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_Product");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__DA638B19D8ACFF81");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Order");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_PaymentMethod");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PK__PaymentM__7BB18931286DB362");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentMethods)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentMethod_Users");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Product__9833FF9258BD5BA6");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ProductStatus).HasDefaultValue("Out stock");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_Category");

            entity.HasOne(d => d.Seller).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_Seller");
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.RegistrationId).HasName("PK__Registra__80C546DF866B4328");

            entity.Property(e => e.RequestedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.Registrations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Registration_Users");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Review__F803F2C392A4FBB4");

            entity.Property(e => e.ReviewDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_Customer");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_Product");
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasKey(e => e.SellerId).HasName("PK__Seller__016148B10CF4E417");

            entity.HasOne(d => d.User).WithMany(p => p.Sellers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Seller_Users");
        });

        modelBuilder.Entity<Shipping>(entity =>
        {
            entity.HasKey(e => e.ShippingId).HasName("PK__Shipping__D9BC449866BBFE72");

            entity.Property(e => e.ShippingId).ValueGeneratedNever();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Shippings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipping_Customer");

            entity.HasOne(d => d.Order).WithMany(p => p.Shippings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipping_Orders");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Shippings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipping_Warehouse");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__206A9DF8F224C413");

            entity.Property(e => e.RegistrationDate).HasDefaultValueSql("(getdate())");
            
            entity.Property(e => e.IsVerified)              
                .HasColumnName("IsVerified")
                .HasDefaultValue(false);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__3D93DCBC31E1CED8");

            entity.HasOne(d => d.Seller).WithMany(p => p.Warehouses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Warehouse_Seller");
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailVer__3214EC07123ABC45");

            entity.Property(e => e.VerificationCode)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.Expiration)
                .HasColumnType("datetime");

            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User)
                .WithMany(p => p.EmailVerifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailVerifications_Users");
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
