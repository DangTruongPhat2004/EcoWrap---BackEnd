using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EcoWrap.DAL;
using EcoWrap.DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class UserAssetUploader
{
    private readonly Cloudinary _cloud;
    private readonly EcoWrapDbContext _db;
    private readonly IConfiguration _cfg;


    public UserAssetUploader(EcoWrapDbContext db, IConfiguration cfg)
    {
        _db = db; _cfg = cfg;
        _cloud = new Cloudinary(new Account(
            _cfg["Storage:Cloudinary:CloudName"],
            _cfg["Storage:Cloudinary:ApiKey"],
            _cfg["Storage:Cloudinary:ApiSecret"]
        ));
    }

    public async Task<UploadUserAssetResponse> UploadAsync(long userId, IFormFile file, string? name, string? tags, CancellationToken ct)
    {
        if (file == null || file.Length == 0) throw new InvalidOperationException("File is required.");
        if (file.Length > 5 * 1024 * 1024) throw new InvalidOperationException("Max 5MB.");

        // Detect loại
        var contentType = file.ContentType?.ToLower() ?? "";
        var ext = Path.GetExtension(file.FileName).Trim('.').ToLower();
        var isPdf = ext == "pdf" || contentType == "application/pdf";
        var isSvg = ext == "svg" || contentType == "image/svg+xml";

        // Slug
        var baseName = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file.FileName) : name!;
        var slug = ToSlug(baseName);
        var prefix = _cfg["Storage:AssetPrefix"] ?? "u";
        var folder = $"{prefix}/{userId}";
        if (string.IsNullOrWhiteSpace(slug)) slug = "file";
        // tránh đụng nhau
        var exists = await _db.UserAssets.AnyAsync(x => x.UserId == userId && x.Slug == slug, ct);
        if (exists) slug += "-" + ShortId();

        var publicId = $"{prefix}/{userId}/{slug}";

        string url, thumb = "", format, resourceType;
        int? width = null, height = null;
        long bytes;

        using var stream = file.OpenReadStream();

        if (isPdf)
        {
            // RAW upload cho PDF
            var up = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = publicId,
                UseFilename = false,
                UniqueFilename = false,
                Overwrite = true
            };

            // Dùng Task.Run để gọi phương thức đồng bộ 'Upload' một cách bất đồng bộ
            var res = await Task.Run(() => _cloud.Upload(up), ct);

            if (res.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Cloudinary upload failed (pdf).");
            resourceType = "raw";
            format = res.Format ?? ext;
            url = res.SecureUrl?.ToString() ?? "";
            bytes = res.Bytes;
        }
        else
        {
            // IMAGE upload cho PNG/JPG/SVG — để có thumbnail
            var up = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),

               
                Folder = folder,            // "u/<userId>"
                PublicId = slug,            // "brand-logo"

                UseFilename = false,
                UniqueFilename = false,
                Overwrite = true,

                EagerTransforms = new List<Transformation> {
        new Transformation().Width(512).Height(512).Crop("fit").FetchFormat("auto").Quality("auto")
    },
                EagerAsync = false
            };
            var res = await _cloud.UploadAsync(up, ct);
            Console.WriteLine($"Uploaded -> PublicId={res.PublicId}, Url={res.SecureUrl}");
            if (res.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Cloudinary upload failed (image).");

            resourceType = "image";
            format = res.Format ?? ext;
            url = res.SecureUrl?.ToString() ?? "";
            bytes = res.Bytes;
            width = res.Width; height = res.Height;
            // eager đầu tiên là thumbnail
            if (res.Eager?.Length > 0) thumb = res.Eager[0].SecureUrl?.ToString() ?? "";
            // fallback nếu vì lý do nào đó không có eager
            if (string.IsNullOrEmpty(thumb))
                thumb = url;
        }

        var asset = new UserAsset
        {
            UserId = userId,
            Name = baseName,
            Slug = slug,
            PublicId = publicId,
            ResourceType = resourceType,
            Format = format,
            Url = url,
            ThumbnailUrl = thumb,
            ContentType = contentType,
            Bytes = bytes,
            Width = width,
            Height = height,
            Tags = tags ?? "",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddHours(7)
        };

        _db.UserAssets.Add(asset);
        await _db.SaveChangesAsync(ct);

        return new UploadUserAssetResponse
        {
            Id = asset.Id,
            Name = asset.Name,
            Url = asset.Url,
            ThumbnailUrl = asset.ThumbnailUrl,
            Format = asset.Format,
            Bytes = asset.Bytes,
            Width = asset.Width,
            Height = asset.Height
        };
    }

    private static string ToSlug(string s)
    {
        s = s.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) sb.Append(ch);
            else if (ch == ' ' || ch == '_' || ch == '-') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug;
    }

    private static string ShortId() => Guid.NewGuid().ToString("N")[..6];
}
