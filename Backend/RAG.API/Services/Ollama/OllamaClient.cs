using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace RAG.API.Services.Ollama
{
    /// <summary>Thin wrapper over the Ollama REST API (/api/chat, /api/embed).</summary>
    public class OllamaClient
    {
        private readonly HttpClient _http;
        private readonly OllamaOptions _options;

        public OllamaClient(HttpClient http, IOptions<OllamaOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<string> ChatAsync(string system, string user, CancellationToken ct = default)
        {
            var request = new
            {
                model = _options.ChatModel,
                stream = false,
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = user },
                },
                options = new { temperature = 0.7, top_p = 0.95 },
            };

            var response = await PostAsync<ChatResponse>("/api/chat", request, ct);
            return response.Message?.Content ?? "No response received.";
        }

        public async Task<List<float[]>> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken ct = default)
        {
            var response = await PostAsync<EmbedResponse>("/api/embed",
                new { model = _options.EmbeddingModel, input = inputs }, ct);
            return response.Embeddings ?? new List<float[]>();
        }

        private async Task<T> PostAsync<T>(string path, object body, CancellationToken ct)
        {
            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsJsonAsync(path, body, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Could not reach Ollama at {_options.BaseUrl}. Is it installed and running ('ollama serve')?", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"Ollama {path} returned {(int)response.StatusCode}: {detail}. " +
                    $"Make sure the models are pulled: 'ollama pull {_options.ChatModel}' and 'ollama pull {_options.EmbeddingModel}'.");
            }

            return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
        }

        private sealed class ChatResponse
        {
            [JsonPropertyName("message")] public ChatMessageDto? Message { get; set; }
        }

        private sealed class ChatMessageDto
        {
            [JsonPropertyName("content")] public string? Content { get; set; }
        }

        private sealed class EmbedResponse
        {
            [JsonPropertyName("embeddings")] public List<float[]>? Embeddings { get; set; }
        }
    }
}
