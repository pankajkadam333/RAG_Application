using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace RAG.API.Services.AzureProvider
{
    public class AzureAIService : IChatService
    {
        private readonly ChatClient _chatClient;

        public AzureAIService(IConfiguration config)
        {
            var endpoint = Required(config, "Azure:OpenAIEndpoint");
            var key = Required(config, "Azure:OpenAIKey");
            var deployment = config["Azure:ChatDeployment"] ?? "gpt-4o";

            var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            _chatClient = client.GetChatClient(deployment);
        }

        public async Task<string> AskQuestionAsync(string context, string question, CancellationToken ct = default)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(Prompts.System),
                new UserChatMessage(Prompts.User(context, question)),
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.7f,
                MaxOutputTokenCount = 800,
                TopP = 0.95f,
            };

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, ct);
            return completion.Content.Count > 0 ? completion.Content[0].Text : "No response received.";
        }

        internal static string Required(IConfiguration config, string key) =>
            string.IsNullOrWhiteSpace(config[key])
                ? throw new InvalidOperationException($"Configuration value '{key}' is required when AI:Provider is 'Azure'.")
                : config[key]!;
    }
}
