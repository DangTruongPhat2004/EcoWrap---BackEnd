namespace EcoWrap.Infrastructure.Storage;

public record StoredFileResult(string PublicUrl, string ContentHash, long SizeBytes, string ContentType);

public interface IFileStorage
{
    /// <summary> Lưu stream lên object storage với key tương đối; tự sinh key nếu null. </summary>
    Task<StoredFileResult> SaveAsync(
        Stream content,
        string contentType,
        string key,                          // vd: "dielines/21/v2.svg" hoặc "u/123/456.png"
        bool immutableCache = false,         // true => Cache-Control: immutable + max-age dài
        CancellationToken ct = default);

    /// <summary> Trả URL công khai tương ứng key (áp CDN nếu có). </summary>
    string GetPublicUrl(string key);
}
