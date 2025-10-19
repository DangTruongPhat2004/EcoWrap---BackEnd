public class StorageOptions
{
    public CloudinaryOptions Cloudinary { get; set; } = new();
}
public class CloudinaryOptions
{
    public string CloudName { get; set; } = default!;
    public string Folder { get; set; } = "home";       
    public string DielinePrefix { get; set; } = "dielines";
    public string AssetsFolder { get; set; } = "Library";
    public Dictionary<string, string> AssetsSubfolders { get; set; } = new()
    {
        ["icon"] = "icons",
        ["pattern"] = "patterns",
        ["illustration"] = "illustrations"
    };
}