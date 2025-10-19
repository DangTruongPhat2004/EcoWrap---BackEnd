// Models/Assets/UploadUserAssetRequest.cs
using Microsoft.AspNetCore.Http;

public class UploadUserAssetRequest
{
    public List<IFormFile> Files { get; set; } = new();
    public string? Tags { get; set; }
}
