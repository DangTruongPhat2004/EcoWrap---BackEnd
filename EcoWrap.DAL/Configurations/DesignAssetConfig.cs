using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DesignAssetConfig : IEntityTypeConfiguration<DesignAsset>
{
    public void Configure(EntityTypeBuilder<DesignAsset> b)
    {
        b.ToTable("DesignAssets");
        b.HasKey(x => x.Id);

        b.Property(x => x.Category)
         .HasConversion<byte>()          // map enum <-> byte
         .HasColumnType("tinyint")       // khớp DB
         .IsRequired();

        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        b.Property(x => x.Tags).HasMaxLength(600);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.Category, x.IsActive });
        b.HasIndex(x => x.CreatedAt);
    }
}