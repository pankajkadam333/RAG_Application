namespace RAG.API.Services
{
    internal static class Prompts
    {
        public const string System =
            "You are an AI assistant. Answer the user's question using the provided document context. " +
            "If the context does not contain the answer, say so.";

        public static string User(string context, string question) =>
            $"Document content:\n{context}\n\nQuestion: {question}";
    }
}
