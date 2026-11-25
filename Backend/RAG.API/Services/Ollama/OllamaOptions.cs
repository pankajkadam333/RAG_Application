namespace RAG.API.Services.Ollama
{
    public class OllamaOptions
    {
        public const string Section = "Ollama";

        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string ChatModel { get; set; } = "llama3.2";
        public string EmbeddingModel { get; set; } = "nomic-embed-text";

        /// <summary>Number of chunks passed to the model as context.</summary>
        public int TopK { get; set; } = 4;

        /// <summary>Chunk size and overlap, in characters.</summary>
        public int ChunkSize { get; set; } = 1000;
        public int ChunkOverlap { get; set; } = 150;

        /// <summary>JSON file holding the local vector index (relative to the app content root).</summary>
        public string StorePath { get; set; } = "App_Data/vector-store.json";

        public int TimeoutSeconds { get; set; } = 300;
    }
}
