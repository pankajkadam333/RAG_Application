using Microsoft.AspNetCore.Mvc;
using RAG.API.Services;

namespace RAG.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly IChatService _chatService;

        public ChatController(ISearchService searchService, IChatService chatService)
        {
            _searchService = searchService;
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Question is required.");

            try
            {
                var relevantChunks = await _searchService.SearchRelevantChunksAsync(request.Message, ct);
                var context = string.Join("\n---\n", relevantChunks);
                var answer = await _chatService.AskQuestionAsync(context, request.Message, ct);
                return Ok(new { question = request.Message, answer });
            }
            catch (InvalidOperationException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}
