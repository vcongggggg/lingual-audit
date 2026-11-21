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

    [HttpGet("scenarios")]
    public async Task<ActionResult<IReadOnlyList<ScenarioDto>>> GetScenarios(
        CancellationToken cancellationToken)
    {
        var result = await _aiTutorService.GetAvailableScenariosAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("roleplay")]
    public async Task<ActionResult<RoleplayTurnResponse>> ProcessRoleplay(
        [FromBody] RoleplayTurnRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return BadRequest(new { message = "Vui lòng nhập câu thoại tiếng Anh để tiếp tục đối thoại." });
        }

        var result = await _aiTutorService.ProcessRoleplayTurnAsync(request, cancellationToken);
        return Ok(result);
    }
}
