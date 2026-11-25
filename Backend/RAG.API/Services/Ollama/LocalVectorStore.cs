using System.Text.Json;
using Microsoft.Extensions.Options;

namespace RAG.API.Services.Ollama
{
    /// <summary>
    /// Minimal in-memory vector index persisted to a JSON file. Fine for local development
    /// with a handful of documents; use Azure AI Search for anything bigger.
    /// </summary>
    public class LocalVectorStore
    {
        private readonly string _path;
        private readonly object _lock = new();
        private List<StoredChunk> _chunks;

        public LocalVectorStore(IOptions<OllamaOptions> options, IWebHostEnvironment env)
        {
            _path = Path.Combine(env.ContentRootPath, options.Value.StorePath);
            _chunks = File.Exists(_path)
                ? JsonSerializer.Deserialize<List<StoredChunk>>(File.ReadAllText(_path)) ?? new()
                : new();
        }

        /// <summary>Replaces all chunks for <paramref name="source"/> and saves to disk.</summary>
        public void Upsert(string source, IReadOnlyList<string> texts, IReadOnlyList<float[]> vectors)
        {
            lock (_lock)
            {
                _chunks.RemoveAll(c => c.Source == source);
                for (var i = 0; i < texts.Count; i++)
                    _chunks.Add(new StoredChunk(source, texts[i], Normalize(vectors[i])));

                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, JsonSerializer.Serialize(_chunks));
            }
        }

        public List<string> Search(float[] query, int topK)
        {
            var q = Normalize(query);
            lock (_lock)
            {
                return _chunks
                    .Where(c => c.Vector.Length == q.Length)
                    .Select(c => (c.Text, Score: Dot(c.Vector, q)))
                    .OrderByDescending(x => x.Score)
                    .Take(topK)
                    .Select(x => x.Text)
                    .ToList();
            }
        }

        private static float Dot(float[] a, float[] b)
        {
            float sum = 0;
            for (var i = 0; i < a.Length; i++) sum += a[i] * b[i];
            return sum;
        }

        private static float[] Normalize(float[] v)
        {
            var norm = MathF.Sqrt(Dot(v, v));
            return norm == 0 ? v : v.Select(x => x / norm).ToArray();
        }

        public record StoredChunk(string Source, string Text, float[] Vector);
    }
}
