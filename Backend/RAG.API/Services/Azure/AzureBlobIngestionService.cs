using Azure.Storage.Blobs;

namespace RAG.API.Services.AzureProvider
{
    /// <summary>
    /// Uploads the file to Blob Storage. An Azure AI Search indexer on the container
    /// (index "AzureSearch:IndexName") is expected to pick it up and chunk it.
    /// </summary>
    public class AzureBlobIngestionService : IDocumentIngestionService
    {
        private readonly BlobContainerClient _container;

        public AzureBlobIngestionService(IConfiguration config)
        {
            _container = new BlobContainerClient(
                AzureAIService.Required(config, "Azure:BlobStorageConnectionString"),
                AzureAIService.Required(config, "Azure:BlobContainer"));
        }

        public async Task<IngestionResult> IngestAsync(IFormFile file, CancellationToken ct = default)
        {
            await _container.CreateIfNotExistsAsync(cancellationToken: ct);
            var blob = _container.GetBlobClient(file.FileName);
            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, overwrite: true, ct);
            return new IngestionResult(file.FileName, Chunks: 0); // chunking happens in the indexer
        }
    }
}
