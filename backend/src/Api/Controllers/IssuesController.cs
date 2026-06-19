using LinearClone.Application.Issues;
using Microsoft.AspNetCore.Mvc;

namespace LinearClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issues;

    public IssuesController(IIssueService issues) => _issues = issues;

    // POST /api/issues
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIssueRequest request, CancellationToken ct)
    {
        var result = await _issues.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // GET /api/issues/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _issues.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // GET /api/issues/team/{teamId}  — list for board/list views
    [HttpGet("team/{teamId:guid}")]
    public async Task<IActionResult> GetByTeam(Guid teamId, CancellationToken ct)
    {
        var result = await _issues.GetByTeamAsync(teamId, ct);
        return Ok(result);
    }

    // PUT /api/issues/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIssueRequest request, CancellationToken ct)
    {
        var result = await _issues.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    // DELETE /api/issues/{id}  — soft delete
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _issues.DeleteAsync(id, ct);
        return NoContent();
    }
}