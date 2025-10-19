// EcoWrap.Infrastructure/Storage/CloudinaryFileStorage.cs
using System.Security.Cryptography;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace EcoWrap.Infrastructure.Storage;

/// <summary>
/// IFileStorage dùng Cloudinary:
/// - Ảnh bitmap (image/* trừ svg): image/upload
/// - SVG & file “raw” (pdf/ai/...): raw/upload (tránh transform)
///
/// Ghi chú:
/// - Nên set cld.Api.Secure = true ở Program.cs để URL luôn https.
/// - Version hoá theo key (ví dụ: .../v1.svg) để tận dụng immutable cache.
/// </summary>
public sealed class CloudinaryFileStorage : IFileStorage
{
    private readonly Cloudinary _cld;
    private readonly string? _folder;
    private readonly int _immutableSeconds; // để đồng bộ interface nếu dùng sau
    private readonly int _staticSeconds;

    public CloudinaryFileStorage(Cloudinary cld, string? folder, int immutableSeconds, int staticSeconds)
    {
        _cld = cld ?? throw new ArgumentNullException(nameof(cld));
        _folder = string.IsNullOrWhiteSpace(folder) ? null : folder.Trim().Trim('/');
        _immutableSeconds = immutableSeconds;
        _staticSeconds = staticSeconds;

        // Guard nhẹ cấu hình hay bị gõ nhầm
        var cloudName = _cld.Api?.Account?.Cloud;
        if (string.IsNullOrWhiteSpace(cloudName) || cloudName.Contains('>') || cloudName.Contains('<'))
            throw new InvalidOperationException("Invalid Cloudinary configuration: cloud name is missing or malformed.");
    }

    public async Task<StoredFileResult> SaveAsync(
        Stream content,
        string contentType,
        string key,
        bool immutableCache = false,
        CancellationToken ct = default)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        ct.ThrowIfCancellationRequested();

        // Chuẩn hoá key/public_id: <folder>/<key> (không slash đầu/cuối)
        var normalizedKey = key.Trim().Trim('/');
        var publicId = string.IsNullOrWhiteSpace(_folder) ? normalizedKey : $"{_folder}/{normalizedKey}";

        // Đọc stream 1 lần -> dùng cho hash & upload
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            if (content.CanSeek) content.Position = 0;
            await content.CopyToAsync(ms, 81920, ct);
            bytes = ms.ToArray();
        }

        // Hash SHA-256 (ETag app-level)
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var hash = "sha256:" + sha;
        var size = (long)bytes.Length;

        // Phân loại: SVG coi như RAW; bitmap image dùng image/upload
        var ctSafe = contentType ?? string.Empty;
        var isSvg = ctSafe.IndexOf("svg", StringComparison.OrdinalIgnoreCase) >= 0
                 || publicId.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        var isBitmapImage = ctSafe.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && !isSvg;

        string? finalUrl;
        string? finalContentType = contentType;

        if (isBitmapImage)
        {
            // ===== Image: image/upload (có UploadAsync hỗ trợ tốt)
            using var uploadStream = new MemoryStream(bytes, writable: false);
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(publicId, uploadStream),
                PublicId = publicId,
                Overwrite = true,
                UseFilename = false,
                UniqueFilename = false,
                Type = "upload"
            };

            var result = await _cld.UploadAsync(uploadParams, ct).ConfigureAwait(false);

            if ((int)result.StatusCode >= 400 || result.Error != null)
                throw new InvalidOperationException($"Cloudinary image upload failed: {result.Error?.Message ?? result.StatusCode.ToString()}");

            finalUrl = (result.SecureUrl ?? result.Url)?.ToString()
                       ?? _cld.Api.UrlImgUp.BuildUrl(publicId);
        }
        else
        {
            // ===== RAW (bao gồm SVG): raw/upload
            // Nhiều phiên bản SDK không có UploadAsync(RawUploadParams) → dùng Upload(...) bọc Task.Run
            using var uploadStream = new MemoryStream(bytes, writable: false);
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(publicId, uploadStream),
                PublicId = publicId,
                Overwrite = true,
                Type = "upload"
            };

            var rawResultObj = await Task.Run(() => _cld.Upload(uploadParams), ct).ConfigureAwait(false);
            var result = rawResultObj as RawUploadResult;

            if (result == null || (int)result.StatusCode >= 400 || result.Error != null)
                throw new InvalidOperationException($"Cloudinary raw upload failed: {result?.Error?.Message ?? result?.StatusCode.ToString() ?? "Unknown"}");

            finalUrl = (result.SecureUrl ?? result.Url)?.ToString()
                       ?? _cld.Api.UrlImgUp.ResourceType("raw").BuildUrl(publicId);

            if (string.IsNullOrWhiteSpace(finalContentType))
                finalContentType = isSvg ? "image/svg+xml" : "application/octet-stream";
        }

        // Ép https nếu vì lý do nào đó URL về http
        if (!string.IsNullOrWhiteSpace(finalUrl) &&
            finalUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            finalUrl = "https://" + finalUrl.Substring("http://".Length);
        }

        if (string.IsNullOrWhiteSpace(finalUrl))
            throw new InvalidOperationException("Cloudinary upload returned empty URL.");

        return new StoredFileResult(finalUrl, hash, size, finalContentType ?? "application/octet-stream");
    }

    public string GetPublicUrl(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

        var normalizedKey = key.Trim().Trim('/');
        var publicId = string.IsNullOrWhiteSpace(_folder) ? normalizedKey : $"{_folder}/{normalizedKey}";

        var lower = normalizedKey.ToLowerInvariant();
        var isBitmapImageExt = lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") ||
                               lower.EndsWith(".webp") || lower.EndsWith(".gif");

        var url = isBitmapImageExt
            ? _cld.Api.UrlImgUp.BuildUrl(publicId)                       // image/upload
            : _cld.Api.UrlImgUp.ResourceType("raw").BuildUrl(publicId);  // raw/upload

        // ép https
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url.Substring("http://".Length);

        return url;
    }
}
