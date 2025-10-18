using EcoWrap.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoWrap.DAL.Configurations;

public class TemplateVersionConfig : IEntityTypeConfiguration<TemplateVersion>
{
    public void Configure(EntityTypeBuilder<TemplateVersion> b)
    {
        b.ToTable("TemplateVersions");
        b.HasKey(x => x.Id);

        b.Property(x => x.VersionNo).IsRequired();
        b.Property(x => x.DielineSvgUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.Model3dUrl).HasMaxLength(500);
        b.Property(x => x.DielineHash).HasMaxLength(100).IsRequired();

        b.Property(x => x.DimensionsUnit).HasMaxLength(10).HasDefaultValue("mm");
        b.Property(x => x.DimensionsWidth).HasColumnType("decimal(9,2)");
        b.Property(x => x.DimensionsHeight).HasColumnType("decimal(9,2)");
        b.Property(x => x.Bleed).HasColumnType("decimal(6,2)");
        b.Property(x => x.SafeArea).HasColumnType("decimal(6,2)");

        b.Property(x => x.Layers).HasMaxLength(200);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2");

        b.HasOne(x => x.Template)
         .WithMany(t => t.Versions)
         .HasForeignKey(x => x.TemplateId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.TemplateId, x.VersionNo }).IsUnique();
        b.HasIndex(x => new { x.TemplateId, x.CreatedAt });
    }
}
