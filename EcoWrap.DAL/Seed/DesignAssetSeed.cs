using Microsoft.EntityFrameworkCore;

public static class DesignAssetSeed
{
    public static void Seed(ModelBuilder mb)
    {
        var t = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        mb.Entity<DesignAsset>().HasData(
          new DesignAsset { Id = 1, Name = "Leaf Outline", Slug = "leaf-outline", Category = AssetCategory.Icon, Tags = "leaf,eco", IsActive = true, CreatedAt = t },
          new DesignAsset { Id = 2, Name = "Recycling", Slug = "recycling", Category = AssetCategory.Icon, Tags = "recycle", IsActive = true, CreatedAt = t },
          new DesignAsset { Id = 3, Name = "Kraft Dots", Slug = "kraft-dots", Category = AssetCategory.Pattern, Tags = "kraft,dots", IsActive = true, CreatedAt = t }
        );
    }
}