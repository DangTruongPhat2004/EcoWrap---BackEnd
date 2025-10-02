namespace EcoWrap.Models.AI
{
    public sealed class RagChatRequest
    {
        public List<ChatMessage> messages { get; set; } = new();
        public int k { get; set; } = 6;
        public string? system { get; set; }
    }
}
