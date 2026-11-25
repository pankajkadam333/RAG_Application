using Microsoft.AspNetCore.Mvc;
using RAG.API.Services;

namespace RAG.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly IDocumentIngestionService _ingestion;

        public FileUploadController(IDocumentIngestionService ingestion)
        {
            _ingestion = ingestion;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return BadRequest("A non-empty file is required.");

            try
            {
                var result = await _ingestion.IngestAsync(file, ct);
                return Ok(new { result.FileName, result.Chunks });
            }
            catch (NotSupportedException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status415UnsupportedMediaType);
            }
            catch (InvalidOperationException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}
