using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

public partial class EcoWrapDbContext : DbContext
{
    public EcoWrapDbContext(DbContextOptions<EcoWrapDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<CarbonFootprint> CarbonFootprints { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Collection> Collections { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<KbChunk> KbChunks { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Registration> Registrations { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Seller> Sellers { get; set; }

    public virtual DbSet<Shipping> Shippings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A");
            entity.Property(e => e.RoleId).HasColumnName("Role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("Role_name");
        });
        
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admins__4A311D2F7766DCBF");

            entity.Property(e => e.AdminId).HasColumnName("Admin_id");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.User).WithMany(p => p.Admins)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Admins_Users");
        });

        modelBuilder.Entity<CarbonFootprint>(entity =>
        {
            entity.HasKey(e => e.CarbonId).HasName("PK__CarbonFo__7D72AC776D488B85");

            entity.ToTable("CarbonFootprint");

            entity.Property(e => e.CarbonId).HasColumnName("Carbon_id");
            entity.Property(e => e.CarbonValue)
                .HasColumnType("decimal(6, 3)")
                .HasColumnName("Carbon_value");
            entity.Property(e => e.DisposalMethod)
                .HasMaxLength(20)
                .HasColumnName("Disposal_method");
            entity.Property(e => e.ProductId)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("Product_id");
            entity.Property(e => e.ProductWeight)
                .HasColumnType("decimal(8, 2)")
                .HasColumnName("Product_weight");
            entity.Property(e => e.WarehouseId).HasColumnName("Warehouse_id");

            entity.HasOne(d => d.Product).WithMany(p => p.CarbonFootprints)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarbonFootprint_Product");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.CarbonFootprints)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarbonFootprint_Warehouse");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__D6862FC1E57BB7D8");

            entity.ToTable("Cart");

            entity.Property(e => e.CartId).HasColumnName("Cart_id");
            entity.Property(e => e.CartTotalPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Cart_total_price");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Created_date");
            entity.Property(e => e.CustomerId).HasColumnName("Customer_id");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Customer).WithMany(p => p.Carts)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cart_Customer");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__64E9CA491B184D0D");

            entity.ToTable("CartItem");

            entity.Property(e => e.CartItemId).HasColumnName("CartItem_id");
            entity.Property(e => e.CartId).HasColumnName("Cart_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("Product_id");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItem_Cart");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItem_Product");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__6DB281361758414C");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId)
                .ValueGeneratedNever()
                .HasColumnName("Category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(50)
                .HasColumnName("Category_name");
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.CollectionId).HasName("PK__Collecti__511BE643C75B4CEC");

            entity.Property(e => e.CollectionId).HasColumnName("Collection_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Created_date");
            entity.Property(e => e.CustomerId).HasColumnName("Customer_id");
            entity.Property(e => e.DesignFile).HasColumnName("Design_file");
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Updated_date");

            entity.HasOne(d => d.Customer).WithMany(p => p.Collections)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Collections_Customer");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId).HasColumnName("Customer_id");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Customers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customer_Users");
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailVer__3214EC074A86A7B6");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Expiration).HasColumnType("datetime");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.UserId).HasColumnName("User_Id");
            entity.Property(e => e.VerificationCode).HasMaxLength(10);

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailVerifications_Users");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__2B66F803DD056717");

            entity.ToTable("Inventory");

            entity.Property(e => e.InventoryId).HasColumnName("Inventory_id");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Last_updated");
            entity.Property(e => e.ProductId)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("Product_id");
            entity.Property(e => e.StockQuantity)
                .HasDefaultValue(0)
                .HasColumnName("Stock_quantity");
            entity.Property(e => e.WarehouseId).HasColumnName("Warehouse_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Product");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Warehouse");
        });

        modelBuilder.Entity<KbChunk>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__KbChunks__3214EC07B3210BE1");

            entity.HasIndex(e => e.Sha256, "IX_KbChunks_Sha256");

            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Sha256).HasMaxLength(64);
            entity.Property(e => e.Source).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(300);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__F1FF8453409B2CFD");

            entity.Property(e => e.OrderId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Order_id");
            entity.Property(e => e.CartId).HasColumnName("Cart_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Created_date");
            entity.Property(e => e.CustomerId).HasColumnName("Customer_id");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .HasColumnName("Order_status");
            entity.Property(e => e.OrderTotalPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Order_total_price");

            entity.HasOne(d => d.Cart).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Cart");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Customer");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__DA638B191B3DEF54");

            entity.ToTable("Payment");

            entity.HasIndex(e => e.TransactionId, "UQ__Payment__9A8C4A3C29BD3E1B").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("Payment_id");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.OrderId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Order_id");
            entity.Property(e => e.PaymentMethodId).HasColumnName("Payment_method_id");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasColumnName("Payment_status");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Transaction_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Order");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_PaymentMethod");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PK__PaymentM__7BB189312B138F7A");

            entity.ToTable("PaymentMethod");

            entity.Property(e => e.PaymentMethodId).HasColumnName("Payment_method_id");
            entity.Property(e => e.MethodAccount)
                .HasMaxLength(255)
                .HasColumnName("Method_account");
            entity.Property(e => e.MethodName)
                .HasMaxLength(50)
                .HasColumnName("Method_name");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentMethods)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentMethod_Users");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Product__9833FF92DE6B6560");

            entity.ToTable("Product");

            entity.Property(e => e.ProductId)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("Product_id");
            entity.Property(e => e.CategoryId).HasColumnName("Category_id");
            entity.Property(e => e.CertificateFile).HasColumnName("Certificate_file");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Created_date");
            entity.Property(e => e.DesignFile).HasColumnName("Design_file");
            entity.Property(e => e.ImageFile).HasColumnName("Image_file");
            entity.Property(e => e.InkType)
                .HasMaxLength(50)
                .HasColumnName("Ink_type");
            entity.Property(e => e.MaterialType)
                .HasMaxLength(50)
                .HasColumnName("Material_type");
            entity.Property(e => e.PrintProcess)
                .HasMaxLength(50)
                .HasColumnName("Print_process");
            entity.Property(e => e.ProductDescription)
                .HasMaxLength(255)
                .HasColumnName("Product_description");
            entity.Property(e => e.ProductName)
                .HasMaxLength(100)
                .HasColumnName("Product_name");
            entity.Property(e => e.ProductPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Product_price");
            entity.Property(e => e.ProductStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Out stock")
                .HasColumnName("Product_status");
            entity.Property(e => e.SellerId).HasColumnName("Seller_id");
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(50);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_Category");

            entity.HasOne(d => d.Seller).WithMany(p => p.Products)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_Seller");
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.RegistrationId).HasName("PK__Registra__80C546DF7D4C348B");

            entity.ToTable("Registration");

            entity.Property(e => e.RegistrationId).HasColumnName("Registration_id");
            entity.Property(e => e.ApprovalStatus)
                .HasMaxLength(20)
                .HasColumnName("Approval_status");
            entity.Property(e => e.CompanyAddress)
                .HasMaxLength(255)
                .HasColumnName("Company_address");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(100)
                .HasColumnName("Company_name");
            entity.Property(e => e.CompanyPhone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Company_phone");
            entity.Property(e => e.DocumentPath).HasColumnName("Document_path");
            entity.Property(e => e.LicenseId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("License_id");
            entity.Property(e => e.PersonalIdImage).HasColumnName("Personal_id_image");
            entity.Property(e => e.RequestedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Requested_date");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.User).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Registration_Users");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Review__F803F2C3EE71D8A9");

            entity.ToTable("Review");

            entity.Property(e => e.ReviewId).HasColumnName("Review_id");
            entity.Property(e => e.CustomerId).HasColumnName("Customer_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("Product_id");
            entity.Property(e => e.ReviewDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Review_date");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_Customer");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_Product");
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasKey(e => e.SellerId).HasName("PK__Seller__016148B160F62C25");

            entity.ToTable("Seller");

            entity.Property(e => e.SellerId).HasColumnName("Seller_id");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CompanyName)
                .HasMaxLength(255)
                .HasColumnName("Company_name");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.User).WithMany(p => p.Sellers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Seller_Users");
        });

        modelBuilder.Entity<Shipping>(entity =>
        {
            entity.HasKey(e => e.ShippingId).HasName("PK__Shipping__D9BC4498C025FFB5");

            entity.ToTable("Shipping");

            entity.Property(e => e.ShippingId)
                .ValueGeneratedNever()
                .HasColumnName("Shipping_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Created_date");
            entity.Property(e => e.CustomerId).HasColumnName("Customer_id");
            entity.Property(e => e.OrderId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Order_id");
            entity.Property(e => e.ShippingAddress)
                .HasMaxLength(255)
                .HasColumnName("Shipping_address");
            entity.Property(e => e.ShippingStatus)
                .HasMaxLength(20)
                .HasColumnName("Shipping_status");
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Updated_date");
            entity.Property(e => e.WarehouseId).HasColumnName("Warehouse_id");

            entity.HasOne(d => d.Customer).WithMany(p => p.Shippings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipping_Customer");

            entity.HasOne(d => d.Order).WithMany(p => p.Shippings)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipping_Orders");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Shippings)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipping_Warehouse");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__206A9DF8");
            entity.Property(e => e.UserId).HasColumnName("User_id");
            entity.Property(e => e.RoleId).HasColumnName("Role_id");
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("Password_hash");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.FullName)
                .HasMaxLength(50)
                .HasColumnName("Full_name");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Phone_number");
            entity.Property(e => e.ProfilePicture).HasColumnName("Profile_picture");
            entity.Property(e => e.RegistrationDate)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Registration_date");
            entity.Property(e => e.IsVerified)
                .HasColumnName("IsVerified")
                .HasDefaultValue(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__3D93DCBCC2960795");

            entity.ToTable("Warehouse");

            entity.Property(e => e.WarehouseId).HasColumnName("Warehouse_id");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.SellerId).HasColumnName("Seller_id");
            entity.Property(e => e.WarehouseName)
                .HasMaxLength(255)
                .HasColumnName("Warehouse_name");

            entity.HasOne(d => d.Seller).WithMany(p => p.Warehouses)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Warehouse_Seller");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}