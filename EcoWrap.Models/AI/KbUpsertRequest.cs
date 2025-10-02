namespace EcoWrap.Models.AI
{
    public sealed class KbUpsertItem
    {
        public string? id { get; set; }
        public string text { get; set; } = "";
        public string? title { get; set; }
        public string? source { get; set; }
        public string? tags { get; set; }
    }
    public sealed class KbUpsertRequest
    {
        public List<KbUpsertItem> items { get; set; } = new();
    }
}
