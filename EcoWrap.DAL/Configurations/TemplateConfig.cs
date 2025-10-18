using EcoWrap.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoWrap.DAL.Configurations;

public class TemplateConfig : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> b)
    {
        b.ToTable("Templates");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.DimensionsUnit).HasMaxLength(10).HasDefaultValue("mm");

        b.Property(x => x.DimensionsWidth).HasColumnType("decimal(9,2)");
        b.Property(x => x.DimensionsHeight).HasColumnType("decimal(9,2)");
        b.Property(x => x.DefaultBleed).HasColumnType("decimal(6,2)").HasDefaultValue(3m);
        b.Property(x => x.DefaultSafeArea).HasColumnType("decimal(6,2)").HasDefaultValue(5m);

        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2");

        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => x.Category);
    }
}
