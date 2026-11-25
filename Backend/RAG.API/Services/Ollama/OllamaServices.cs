using System.Text;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace RAG.API.Services.Ollama
{
    public class OllamaChatService : IChatService
    {
        private readonly OllamaClient _client;

        public OllamaChatService(OllamaClient client) => _client = client;

        public Task<string> AskQuestionAsync(string context, string question, CancellationToken ct = default) =>
            _client.ChatAsync(Prompts.System, Prompts.User(context, question), ct);
    }

    public class LocalSearchService : ISearchService
    {
        private readonly OllamaClient _client;
        private readonly LocalVectorStore _store;
        private readonly OllamaOptions _options;

        public LocalSearchService(OllamaClient client, LocalVectorStore store, IOptions<OllamaOptions> options)
        {
            _client = client;
            _store = store;
            _options = options.Value;
        }

        public async Task<List<string>> SearchRelevantChunksAsync(string query, CancellationToken ct = default)
        {
            var vectors = await _client.EmbedAsync(new[] { query }, ct);
            return vectors.Count == 0 ? new List<string>() : _store.Search(vectors[0], _options.TopK);
        }
    }

    /// <summary>Extracts text, chunks it, embeds with Ollama and stores it in the local vector store.</summary>
    public class LocalIngestionService : IDocumentIngestionService
    {
        private static readonly HashSet<string> TextExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".txt", ".md", ".csv", ".json", ".xml", ".html", ".htm" };

        private readonly OllamaClient _client;
        private readonly LocalVectorStore _store;
        private readonly OllamaOptions _options;

        public LocalIngestionService(OllamaClient client, LocalVectorStore store, IOptions<OllamaOptions> options)
        {
            _client = client;
            _store = store;
            _options = options.Value;
        }

        public static bool IsSupported(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            return ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) || TextExtensions.Contains(ext);
        }

        public async Task<IngestionResult> IngestAsync(IFormFile file, CancellationToken ct = default)
        {
            if (!IsSupported(file.FileName))
                throw new NotSupportedException(
                    $"'{Path.GetExtension(file.FileName)}' files are not supported in local (Ollama) mode. Use PDF or plain-text files.");

            var text = await ExtractTextAsync(file, ct);
            var chunks = Chunk(text, _options.ChunkSize, _options.ChunkOverlap);
            if (chunks.Count == 0)
                return new IngestionResult(file.FileName, 0);

            var vectors = new List<float[]>();
            foreach (var batch in chunks.Chunk(32))
                vectors.AddRange(await _client.EmbedAsync(batch, ct));

            _store.Upsert(file.FileName, chunks, vectors);
            return new IngestionResult(file.FileName, chunks.Count);
        }

        private static async Task<string> ExtractTextAsync(IFormFile file, CancellationToken ct)
        {
            await using var stream = file.OpenReadStream();
            if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return await reader.ReadToEndAsync(ct);
            }

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            using var pdf = PdfDocument.Open(ms.ToArray());
            return string.Join("\n\n", pdf.GetPages().Select(p => p.Text));
        }

        internal static List<string> Chunk(string text, int size, int overlap)
        {
            text = text.Replace("\r\n", "\n").Trim();
            var chunks = new List<string>();
            var step = Math.Max(1, size - overlap);
            for (var start = 0; start < text.Length; start += step)
            {
                var chunk = text.Substring(start, Math.Min(size, text.Length - start)).Trim();
                if (chunk.Length > 0) chunks.Add(chunk);
                if (start + size >= text.Length) break;
            }
            return chunks;
        }
    }
}
