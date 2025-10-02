using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace EcoWrap.BLL.Services
{
    public class EmbeddingService : EcoWrap.BLL.IServices.IEmbeddingService
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;

        public EmbeddingService(IHttpClientFactory http, IConfiguration cfg)
        { _http = http; _cfg = cfg; }

        public async Task<float[]> EmbedAsync(string text, string taskType = "RETRIEVAL_DOCUMENT")
        {
            var key = _cfg["GoogleAI:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Missing GOOGLE_API_KEY");

            var client = _http.CreateClient("googleai");

            // ĐÚNG endpoint + payload
            var model = "models/text-embedding-004";
            var url = $"v1beta/{model}:embedContent?key={key}";
            var payload = new
            {
                content = new { parts = new[] { new { text } } },
                task_type =  taskType // hoặc RETRIEVAL_QUERY khi embed câu hỏi
            };

            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var res = await client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Embeddings error {(int)res.StatusCode}: {body}");

            using var json = JsonDocument.Parse(body);
            var values = json.RootElement.GetProperty("embedding").GetProperty("values");
            return values.EnumerateArray().Select(v => v.GetSingle()).ToArray();
        }
    }
}
