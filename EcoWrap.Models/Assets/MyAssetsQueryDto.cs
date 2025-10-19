// Models/Assets/MyAssetsQueryDto.cs
public class MyAssetsQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }      // tìm theo Name/Tags
    public string? Format { get; set; }      // png|jpg|jpeg|svg|pdf...
    public string? Type { get; set; }        // image|raw
    public Guid? UserId { get; set; }        // tạm dùng khi CHƯA bật auth
}

