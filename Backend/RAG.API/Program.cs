using RAG.API.Services;
using RAG.API.Services.AzureProvider;
using RAG.API.Services.Ollama;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// AI provider switch: "Ollama" (local) or "Azure" (Azure OpenAI + AI Search + Blob Storage).
// Set via appsettings*.json, or the env var AI__Provider at deploy time.
var provider = builder.Configuration["AI:Provider"] ?? AiProviders.Ollama;

if (provider.Equals(AiProviders.Ollama, StringComparison.OrdinalIgnoreCase))
{
    builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.Section));
    builder.Services.AddHttpClient<OllamaClient>((sp, http) =>
    {
        var options = builder.Configuration.GetSection(OllamaOptions.Section).Get<OllamaOptions>() ?? new();
        http.BaseAddress = new Uri(options.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    });
    builder.Services.AddSingleton<LocalVectorStore>();
    builder.Services.AddScoped<IChatService, OllamaChatService>();
    builder.Services.AddScoped<ISearchService, LocalSearchService>();
    builder.Services.AddScoped<IDocumentIngestionService, LocalIngestionService>();
}
else if (provider.Equals(AiProviders.Azure, StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IChatService, AzureAIService>();
    builder.Services.AddScoped<ISearchService, AzureSearchService>();
    builder.Services.AddScoped<IDocumentIngestionService, AzureBlobIngestionService>();
}
else
{
    throw new InvalidOperationException(
        $"Unknown AI:Provider '{provider}'. Use '{AiProviders.Ollama}' or '{AiProviders.Azure}'.");
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.Logger.LogInformation("AI provider: {Provider}", provider);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");

app.UseAuthorization();

app.MapControllers();

app.Run();
