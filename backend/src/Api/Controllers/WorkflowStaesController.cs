using Microsoft.AspNetCore.Authorization;
using LinearClone.Application.WorkflowStates;
using Microsoft.AspNetCore.Mvc;

namespace LinearClone.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkflowStatesController : ControllerBase
{
    private readonly IWorkflowStateService _states;

    public WorkflowStatesController(IWorkflowStateService states) => _states = states;

    // GET /api/workflowstates/team/{teamId}
    [HttpGet("team/{teamId:guid}")]
    public async Task<IActionResult> GetByTeam(Guid teamId, CancellationToken ct)
    {
        var result = await _states.GetByTeamAsync(teamId, ct);
        return Ok(result);
    }
}