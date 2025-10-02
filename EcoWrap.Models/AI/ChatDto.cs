namespace EcoWrap.Models.AI
{
    public sealed class ChatMessage
    {
        public string role { get; set; } = "user"; // "user" | "assistant"
        public string content { get; set; } = "";
    }
    public sealed class ChatRequest
    {
        public List<ChatMessage> messages { get; set; } = new();
    }
    public sealed class ChatResponse
    {
        public string reply { get; set; } = "";
        public bool? error { get; set; }
        public string? message { get; set; }
        public int? status { get; set; }
        public string? detail { get; set; }
    }
}
