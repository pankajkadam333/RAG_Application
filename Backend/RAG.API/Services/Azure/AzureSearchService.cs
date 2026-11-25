using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace RAG.API.Services.AzureProvider
{
    public class AzureSearchService : ISearchService
    {
        private readonly SearchClient _searchClient;

        public AzureSearchService(IConfiguration config)
        {
            var endpoint = AzureAIService.Required(config, "AzureSearch:Endpoint");
            var indexName = AzureAIService.Required(config, "AzureSearch:IndexName");
            var apiKey = AzureAIService.Required(config, "AzureSearch:ApiKey");

            _searchClient = new SearchClient(new Uri(endpoint), indexName, new AzureKeyCredential(apiKey));
        }

        public async Task<List<string>> SearchRelevantChunksAsync(string query, CancellationToken ct = default)
        {
            var options = new SearchOptions { IncludeTotalCount = true };
            options.Select.Add("content");

            var response = await _searchClient.SearchAsync<SearchDocument>(query, options, ct).ConfigureAwait(false);

            return response.Value.GetResults()
                .Select(r => r.Document.TryGetValue("content", out var content) ? content?.ToString() : null)
                .Where(text => !string.IsNullOrEmpty(text))
                .Select(text => text!)
                .ToList();
        }
    }
}
