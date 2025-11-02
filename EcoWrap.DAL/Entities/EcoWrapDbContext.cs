using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;


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

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Collection> Collections { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<DesignAsset> DesignAssets { get; set; }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<KbChunk> KbChunks { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Registration> Registrations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Seller> Sellers { get; set; }



    public virtual DbSet<User> Users { get; set; }

    // Chat DbSets
    public virtual DbSet<Conversation> Conversations { get; set; }
    public virtual DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    public virtual DbSet<Message> Messages { get; set; }
    public virtual DbSet<MessageReceipt> MessageReceipts { get; set; }

    

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    // Template

    public DbSet<Template> Templates => Set<Template>();
    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();
    public DbSet<UserAsset> UserAssets { get; set; } = default!;

    //design
    public DbSet<Custom> Customs => Set<Custom>();
    public DbSet<CustomAsset> CustomAssets => Set<CustomAsset>();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
       => optionsBuilder.UseSqlServer("Server=localhost;uid=sa;pwd=123456;Database=EcoWrap;Trusted_Connection=True;TrustServerCertificate=True;");
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // luôn đặt ở đầu, KHÔNG đặt trong từng entity
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Template>(e =>
        {
            e.ToTable("Templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            e.Property(x => x.DimensionsUnit).HasMaxLength(10).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("getutcdate()");
            e.HasMany(x => x.Versions).WithOne(v => v.Template)
                .HasForeignKey(v => v.TemplateId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateVersion>(e =>
        {
            e.ToTable("TemplateVersions");
            e.HasKey(x => x.Id); // PK đơn
            e.HasIndex(x => new { x.TemplateId, x.VersionNo }).IsUnique(); // đảm bảo duy nhất
            e.Property(x => x.DielineSvgUrl).HasMaxLength(1000).IsRequired();
            e.Property(x => x.DielineHash).HasMaxLength(128).IsRequired();
            e.Property(x => x.DimensionsUnit).HasMaxLength(10).IsRequired();
            e.Property(x => x.Layers).HasMaxLength(400);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("getutcdate()");
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admins__4A311D2FC2A7F007");

            entity.HasOne(d => d.User).WithMany(p => p.Admins)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Admins_Users");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Cart");
            entity.HasKey(e => e.CartId).HasName("PK__Cart__D6862FC1985FD2E5");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Customer).WithMany(p => p.Carts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cart_Customer");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__64E9CA49670BA488");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItem_Cart");

            entity.HasOne(d => d.Order).WithMany(p => p.CartItems).HasConstraintName("FK_CartItem_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItem_Product");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__6DB281368D8EAF25");

            entity.Property(e => e.CategoryId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.CollectionId).HasName("PK__Collecti__511BE643CDFE2B31");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Collections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Collections_Customer");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__8CB382B1B5A484C4");

            entity.HasOne(d => d.User).WithMany(p => p.Customers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customer_Users");
        });

        modelBuilder.Entity<DesignAsset>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailVer__3214EC07E4B5D646");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailVerifications_Users");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__Feedback_id");

            entity.Property(e => e.FeedbackDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Feedback_date");

            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.Property(e => e.Rating).HasColumnType("tinyint");

            entity.HasOne(d => d.User) 
                .WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId) 
                .OnDelete(DeleteBehavior.ClientSetNull) 
                .HasConstraintName("FK_Feedback_Users"); 
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__2B66F80344B0F812");

            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Product");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Inventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Warehouse");
        });

        var floatArrayConverter = new ValueConverter<float[]?, byte[]?>(
             v => v == null ? null : FloatArrayToBytes(v),
             v => v == null ? null : BytesToFloatArray(v)
         );

        var floatArrayComparer = new ValueComparer<float[]?>(
            (a, b) =>
                (a == null && b == null) ||
                (a != null && b != null && a.SequenceEqual(b)),
            v => v == null ? 0 : v.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())),
            v => v == null ? null : v.ToArray()
        );

        modelBuilder.Entity<KbChunk>()
            .Property(p => p.Embedding)
            .HasConversion(floatArrayConverter)
            .Metadata.SetValueComparer(floatArrayComparer);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__F1FF8453EDE50EC7");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Customer");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__DA638B1913F0C6B2");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments).HasConstraintName("FK_Payment_Order");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Product__9833FF925F574BCC");

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
            entity.HasKey(e => e.RegistrationId).HasName("PK__Registra__80C546DFDD20D2B5");

            entity.Property(e => e.RequestedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.Registrations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Registration_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__D80BB093D6EE88A2");

            entity.Property(e => e.RoleId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasKey(e => e.SellerId).HasName("PK__Seller__016148B11626CEAE");

            entity.HasOne(d => d.User).WithMany(p => p.Sellers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Seller_Users");
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Template__3214EC0734C86B01");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<TemplateVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Template__3214EC074DF50F78");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Template).WithMany(p => p.Versions).HasConstraintName("FK_TemplateVersions_Templates");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__206A9DF8B7C71E58");

            entity.Property(e => e.RegistrationDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Role).WithMany(p => p.Users).HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<UserAsset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserAsse__3214EC07A9B1E336");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__3D93DCBC8488B8D7");

            // <<< BỔ SUNG CẤU HÌNH ĐỘ DÀI TRƯỜNG >>>
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.ProvinceId).HasMaxLength(255);
            entity.Property(e => e.DistrictId).HasMaxLength(255);
            entity.Property(e => e.WardId).HasMaxLength(255);
            // <<< HẾT BỔ SUNG >>>

            entity.HasOne(d => d.Seller).WithMany(p => p.Warehouses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Warehouse_Seller");
        });

        modelBuilder.Entity<Conversation>(b =>
        {
            b.ToTable("Conversations");
            b.HasKey(x => x.Id);
            b.Property(x => x.LastMessagePreview).HasMaxLength(200);
            b.HasIndex(x => x.LastMessageAt);
        });

        modelBuilder.Entity<ConversationParticipant>(b =>
        {
            b.ToTable("ConversationParticipants");
            b.HasKey(x => new { x.ConversationId, x.UserId });
            b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            b.HasOne(x => x.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(x => x.ConversationId);
            b.HasOne(x => x.User)
                .WithMany() // nếu User có navigation thì thay cho phù hợp
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Message>(b =>
        {
            b.ToTable("Messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.ContentType).HasConversion<string>().HasMaxLength(30);
            b.Property(x => x.AttachmentUrl).HasMaxLength(500);
            b.Property(x => x.AttachmentName).HasMaxLength(255);
            b.HasIndex(x => new { x.ConversationId, x.CreatedAt });
            b.HasOne(x => x.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(x => x.ConversationId);
        });

        modelBuilder.Entity<MessageReceipt>(b =>
        {
            b.ToTable("MessageReceipts");
            b.HasKey(x => new { x.MessageId, x.UserId });
            b.HasOne(x => x.Message)
                .WithMany(m => m.Receipts)
                .HasForeignKey(x => x.MessageId);
            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Custom>(e =>
        {
            e.ToTable("Designs", tb =>
            {
                tb.HasTrigger("trg_Designs_SetUpdatedAt");   // 👈 tên trigger đúng như bạn đã tạo
                                                             // nếu có nhiều trigger: tb.HasTrigger("TênTriggerKhác");
            });

            e.HasIndex(d => new { d.OwnerId, d.UpdatedAt });
            e.Property(d => d.RowVersion).IsRowVersion();
            e.Property(d => d.DesignData).HasColumnType("nvarchar(max)");
            e.Property(d => d.TagsJson).HasColumnType("nvarchar(max)");

            e.HasMany(d => d.Assets)
             .WithOne(a => a.Custom)
             .HasForeignKey(a => a.CustomId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== CustomAsset (map bảng CustomAssets) =====
        modelBuilder.Entity<CustomAsset>(e =>
        {
            e.ToTable("CustomAssets");           // ✅ bảng riêng
            e.HasIndex(a => a.CustomId);
            e.Property(a => a.Url).HasMaxLength(2048);
        });

        modelBuilder.ApplyConfiguration(new DesignAssetConfig());
        DesignAssetSeed.Seed(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    // ===== Helpers cho converter (đặt cấp lớp, KHÔNG để local trong OnModelCreating) =====
    private static byte[] FloatArrayToBytes(float[] arr)
    {
        var bytes = new byte[arr.Length * sizeof(float)];
        Buffer.BlockCopy(arr, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BytesToFloatArray(byte[] bytes)
    {
        var arr = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, arr, 0, bytes.Length);
        return arr;
    }
}