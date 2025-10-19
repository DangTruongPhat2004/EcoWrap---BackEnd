using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Security.Cryptography;

namespace EcoWrap.Infrastructure.Storage;

public sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;
    private readonly string? _cdnBase;
    private readonly int _immutableSeconds;
    private readonly int _staticSeconds;

    public AzureBlobFileStorage(BlobContainerClient container, string? cdnBase, int immutableSeconds, int staticSeconds)
    {
        _container = container;
        _cdnBase = string.IsNullOrWhiteSpace(cdnBase) ? null : cdnBase!.TrimEnd('/');
        _immutableSeconds = immutableSeconds;
        _staticSeconds = staticSeconds;
    }

    public async Task<StoredFileResult> SaveAsync(Stream content, string contentType, string key, bool immutableCache = false, CancellationToken ct = default)
    {
        // Azure: "key" = blob name
        var blob = _container.GetBlobClient(key);

        // Tính sha256
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

            var headers = new BlobHttpHeaders
            {
                ContentType = contentType,
                // Cache-Control
                CacheControl = immutableCache
                    ? $"public, max-age={_immutableSeconds}, immutable"
                    : $"public, max-age={_staticSeconds}"
            };

            await blob.UploadAsync(ms, new BlobUploadOptions { HttpHeaders = headers }, ct);
        }

        var url = GetPublicUrl(key);
        return new StoredFileResult(url, sha256, sizeBytes, contentType);
    }

    public string GetPublicUrl(string key)
    {
        if (_cdnBase is not null) return $"{_cdnBase}/{key}";
        // fallback direct blob url (không khuyến nghị cho prod public)
        return _container.GetBlobClient(key).Uri.ToString();
    }

    public static BlobContainerClient CreateContainer(string connectionString, string containerName)
    {
        var client = new BlobContainerClient(connectionString, containerName);
        client.CreateIfNotExists(PublicAccessType.Blob);
        return client;
    }
}
