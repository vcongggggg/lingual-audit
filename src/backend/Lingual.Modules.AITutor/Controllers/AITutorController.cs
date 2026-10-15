using Microsoft.AspNetCore.Mvc;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;

namespace Lingual.Modules.AITutor.Controllers;

[ApiController]
[Route("api/v1/ai")]
public class AITutorController : ControllerBase
{
    private readonly IAITutorService _aiTutorService;

    public AITutorController(IAITutorService aiTutorService)
    {
        _aiTutorService = aiTutorService;
    }

    [HttpPost("grammar-check")]
    public async Task<ActionResult<GrammarAnalysisResponse>> CheckGrammar(
        [FromBody] GrammarAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { message = "Vui lòng cung cấp câu tiếng Anh cần kiểm tra." });
        }

        var result = await _aiTutorService.AnalyzeGrammarAsync(request, cancellationToken);
        return Ok(result);
    }
}
