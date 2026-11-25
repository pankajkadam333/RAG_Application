namespace RAG.API.Services
{
    /// <summary>Generates an answer from a question plus retrieved document context.</summary>
    public interface IChatService
    {
        Task<string> AskQuestionAsync(string context, string question, CancellationToken ct = default);
    }

    /// <summary>Finds document chunks relevant to a query.</summary>
    public interface ISearchService
    {
        Task<List<string>> SearchRelevantChunksAsync(string query, CancellationToken ct = default);
    }

    /// <summary>Stores an uploaded document so it becomes searchable.</summary>
    public interface IDocumentIngestionService
    {
        Task<IngestionResult> IngestAsync(IFormFile file, CancellationToken ct = default);
    }

    public record IngestionResult(string FileName, int Chunks);

    public static class AiProviders
    {
        public const string Ollama = "Ollama";
        public const string Azure = "Azure";
    }
}
