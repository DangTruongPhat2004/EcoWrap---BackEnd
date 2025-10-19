using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Security.Cryptography;
using System.Text;

namespace EcoWrap.Infrastructure.Storage;

public sealed class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string? _cdnBase;
    private readonly int _immutableSeconds;
    private readonly int _staticSeconds;

    public S3FileStorage(IAmazonS3 s3, string bucket, string? cdnBase, int immutableSeconds, int staticSeconds)
    {
        _s3 = s3;
        _bucket = bucket;
        _cdnBase = string.IsNullOrWhiteSpace(cdnBase) ? null : cdnBase!.TrimEnd('/');
        _immutableSeconds = immutableSeconds;
        _staticSeconds = staticSeconds;
    }

    public async Task<StoredFileResult> SaveAsync(Stream content, string contentType, string key, bool immutableCache = false, CancellationToken ct = default)
    {
        // Tính SHA256 để lưu DB làm DielineHash/hợp lệ hoá ETag app-level
        string sha256;
        long sizeBytes;
        using (var ms = new MemoryStream())
        {
            await content.CopyToAsync(ms, ct);
            sizeBytes = ms.Length;
            ms.Position = 0;
            using var sha = SHA256.Create();
            sha256 = "sha256:" + Convert.ToHexString(sha.ComputeHash(ms)).ToLowerInvariant();
            ms.Position = 0;

            var put = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = ms,
                ContentType = contentType,
                AutoCloseStream = false
            };

            // Cache policy
            put.Headers.CacheControl = immutableCache
                ? $"public, max-age={_immutableSeconds}, immutable"
                : $"public, max-age={_staticSeconds}";

            // (tùy chọn) ContentDisposition nếu muốn gợi ý tên file tải xuống
            // put.Headers.ContentDisposition = "inline"; 

            var resp = await _s3.PutObjectAsync(put, ct);
        }

        var url = GetPublicUrl(key);
        return new StoredFileResult(url, sha256, sizeBytes, contentType);
    }

    public string GetPublicUrl(string key)
    {
        if (_cdnBase is not null) return $"{_cdnBase}/{key}";
        // fallback S3 website/canonical (tuỳ cấu hình, demo dạng virtual-hosted–style):
        return $"https://{_bucket}.s3.amazonaws.com/{key}";
    }

    public static IAmazonS3 CreateClient(string region, string accessKey, string secretKey)
        => new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
}
