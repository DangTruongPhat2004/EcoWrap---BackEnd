using Microsoft.Extensions.Configuration;

public interface IAssetUrlBuilder
{
    (string url, string thumb) Build(string category, string slug, int? v = null);
}

public class AssetUrlBuilder : IAssetUrlBuilder
{
    private readonly IConfiguration _cfg;
    public AssetUrlBuilder(IConfiguration cfg) => _cfg = cfg;

    public (string url, string thumb) Build(string category, string slug, int? v = null)
    {
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("slug is required");

        var cloud = _cfg["Storage:Cloudinary:CloudName"]!;
        var root = (_cfg["Storage:Cloudinary:AssetsFolder"] ?? "").Trim('/');
        var map = _cfg.GetSection("Storage:Cloudinary:AssetsSubfolders");
        var sub = !string.IsNullOrWhiteSpace(map[category]) ? map[category]! : category;

        // chọn raw/image theo cấu hình
        var svgType = (_cfg["Storage:Cloudinary:SvgUploadType"] ?? "raw").Trim().ToLower();
        var svgBase = svgType == "image" ? "image" : "raw";

        var ver = v is > 0 ? $"v{v}/" : "";
        var basePath = string.IsNullOrEmpty(root) ? $"{sub}/{slug}" : $"{root}/{sub}/{slug}";

        var svgUrl = $"https://res.cloudinary.com/{cloud}/{svgBase}/upload/{ver}{basePath}.svg";
        var thumb = $"https://res.cloudinary.com/{cloud}/image/upload/{ver}{basePath}_thumb.png";
        return (svgUrl, thumb);
    }
}
