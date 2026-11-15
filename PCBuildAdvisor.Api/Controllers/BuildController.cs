using Microsoft.AspNetCore.Mvc;
using PCBuildAdvisor.Api.DTOs;
using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Services;
using PCBuildAdvisor.Api.Services.Interfaces;

namespace PCBuildAdvisor.Api.Controllers;

[ApiController]
[Route("api")]
public class BuildController(IBuildAdvisorService advisor, CatalogService catalog) : ControllerBase
{
    [HttpGet("games")]
    public async Task<IActionResult> GetGames(CancellationToken cancellationToken) =>
        Ok(await catalog.GetGamesAsync(cancellationToken));

    [HttpGet("components")]
    public IActionResult GetComponents([FromQuery] ComponentType? type)
    {
        if (type.HasValue && !Enum.IsDefined(type.Value))
            return BadRequest(new { message = "The component type is invalid." });
        return Ok(catalog.GetComponents(type));
    }

    [HttpPost("builds/recommend")]
    public async Task<ActionResult<BuildRecommendation>> Recommend([FromBody] BuildRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await advisor.GenerateBuildAsync(request, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (BuildSelectionException ex)
        {
            return UnprocessableEntity(new { message = ex.Message, minimumEstimatedTotal = ex.MinimumEstimatedTotal });
        }
    }

    [HttpPost("builds/evaluate")]
    public ActionResult<BuildRecommendation> Evaluate([FromBody] ComputerBuild build)
    {
        if (build is null) return BadRequest(new { message = "A build is required." });
        try { return Ok(advisor.EvaluateBuild(build)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
