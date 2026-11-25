# Document AI Chat — RAG Application

Chat with your own documents. Upload a file, ask questions in plain language, and get answers grounded in what the document actually says, instead of the model's general knowledge.

## What this project does

This is a **Retrieval-Augmented Generation (RAG)** application:

1. **Upload**: you upload a document (PDF, text, …).
2. **Index**: the document's text is split into chunks, and each chunk is stored in a searchable index.
3. **Retrieve**: when you ask a question, the most relevant chunks are found.
4. **Generate**: those chunks and your question are sent to a large language model, which writes an answer using only that context.

```
 Angular UI ──► ASP.NET Core API ──► Search (find relevant chunks)
 (port 4200)    (port 7139)      └─► LLM (answer from those chunks)
```

### Goals

- **Answers grounded in your data.** The model answers from your documents, so it makes up less and can work with private or internal content it was never trained on.
- **Free, offline local development.** Locally, everything runs on your machine with [Ollama](https://ollama.com). You don't need cloud keys, it costs nothing, and documents never leave your computer.
- **Production on Azure.** When deployed, the same API switches to Azure OpenAI, Azure AI Search and Azure Blob Storage through configuration only, with no code changes.

## Two AI providers, one setting

The backend chooses its AI stack from `AI:Provider`:

| | `Ollama` (local) | `Azure` (deployment) |
|---|---|---|
| Answers (LLM) | Ollama `llama3.2` | Azure OpenAI (`gpt-4o` deployment) |
| Embeddings | Ollama `nomic-embed-text` | handled by the Azure AI Search index |
| Search index | Local JSON file `App_Data/vector-store.json` (cosine similarity) | Azure AI Search (`azureblob-index`) |
| Upload storage | Parsed and indexed directly by the API | Azure Blob Storage, picked up by an AI Search indexer |
| Supported files | PDF and plain text (`.txt`, `.md`, `.csv`, `.json`, `.xml`, `.html`) | Whatever your indexer handles |
| Default when | `ASPNETCORE_ENVIRONMENT=Development` | Any other environment |

`appsettings.Development.json` sets `Ollama`, and `appsettings.json` sets `Azure`.

## Project structure

```
RAGApplication/
├── Backend/
│   └── RAG.API/                  ASP.NET Core Web API
│       ├── Controllers/
│       │   ├── ChatController.cs        POST /api/chat
│       │   └── FileUploadController.cs  POST /api/FileUpload
│       ├── Services/
│       │   ├── Abstractions.cs          IChatService, ISearchService, IDocumentIngestionService
│       │   ├── Azure/                   Azure OpenAI, AI Search, Blob Storage
│       │   └── Ollama/                  Ollama client, local vector store, PDF/text ingestion
│       ├── Program.cs                   Picks the provider from AI:Provider
│       └── appsettings*.json
└── Frontend/                     Angular 19 single-page app (Home, Chat, Documents)
```

## Run it locally

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download): the project targets .NET 8. If you only have a newer runtime (for example .NET 10), set `DOTNET_ROLL_FORWARD=Major` as shown below.
- [Node.js](https://nodejs.org) (an LTS version is recommended) and npm
- [Ollama](https://ollama.com/download)

### 1. Install Ollama and pull the models (one time)

```powershell
winget install Ollama.Ollama
ollama pull llama3.2
ollama pull nomic-embed-text
```

Make sure Ollama is running. The desktop app starts it automatically; otherwise run `ollama serve`. You can check it with:

```powershell
curl http://localhost:11434/api/version
```

### 2. Trust the HTTPS development certificate (one time)

```powershell
dotnet dev-certs https --trust
```

### 3. Start the backend API

```powershell
cd Backend/RAG.API
$env:DOTNET_ROLL_FORWARD = "Major"   # only needed if .NET 8 runtime is not installed
dotnet run --launch-profile https
```

The API listens on **https://localhost:7139**. Swagger UI is at https://localhost:7139/swagger. The log line `AI provider: Ollama` confirms the local mode.

### 4. Start the frontend

In a second terminal:

```powershell
cd Frontend
npm install
npm start
```

Open **http://localhost:4200**.

### 5. Try it

1. Go to **Documents** and upload a PDF or text file.
2. Go to **Chat** and ask a question about it.

Or use the API directly:

```powershell
curl.exe -k -F "file=@my-notes.pdf" https://localhost:7139/api/FileUpload
curl.exe -k -X POST https://localhost:7139/api/chat -H "Content-Type: application/json" -d '{\"message\":\"What is this document about?\"}'
```

To clear the local index, stop the API and delete `Backend/RAG.API/App_Data/vector-store.json`.

## Configuration

### Ollama settings (`Ollama` section)

| Key | Default | Meaning |
|---|---|---|
| `BaseUrl` | `http://localhost:11434` | Ollama server address |
| `ChatModel` | `llama3.2` | Model that writes answers. Larger models follow the documents more closely. |
| `EmbeddingModel` | `nomic-embed-text` | Model used to turn chunks into vectors |
| `TopK` | `4` | How many chunks are sent as context |
| `ChunkSize` / `ChunkOverlap` | `1000` / `150` | Chunk size and overlap, in characters |
| `StorePath` | `App_Data/vector-store.json` | Local index file |
| `TimeoutSeconds` | `300` | Request timeout to Ollama |

If you change `EmbeddingModel`, delete the index file and upload your documents again, because vectors from different models can't be compared.

### Deploying with Azure

Set `AI:Provider` to `Azure` (the default outside Development) and fill in the Azure values. Use environment variables or a secret store rather than committing keys:

```
AI__Provider=Azure
Azure__OpenAIEndpoint=https://<your-resource>.openai.azure.com/
Azure__OpenAIKey=<key>
Azure__ChatDeployment=gpt-4o
Azure__BlobStorageConnectionString=<connection string>
Azure__BlobContainer=<container name>
AzureSearch__Endpoint=https://<your-search>.search.windows.net
AzureSearch__IndexName=azureblob-index
AzureSearch__ApiKey=<key>
Cors__AllowedOrigins__0=https://<your-frontend-host>
```

In Azure mode, you need an Azure AI Search **indexer** on the blob container that writes the document text into a `content` field of the index. Uploads go to Blob Storage, and the indexer makes them searchable.

The frontend currently calls `https://localhost:7139/api` (see `Frontend/src/app/services/azure-ai.service.ts` and `document.service.ts`). Change that URL to your deployed API's address before deploying.

## Known limitations

- The local vector store is a single JSON file loaded into memory. It is meant for development with a handful of documents, not for large collections.
- Word, PowerPoint, Excel and image files are not parsed in Ollama mode.
- The Documents page still shows demo entries and a simulated progress bar. The real upload and indexing happen in the background.
