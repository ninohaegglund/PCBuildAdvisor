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
    public async Task<IActionResult> GetGames() => Ok(await catalog.GetGamesAsync());

    [HttpGet("components")]
    public IActionResult GetComponents([FromQuery] ComponentType? type) => Ok(catalog.GetComponents(type));

    [HttpPost("builds/recommend")]
    public ActionResult<BuildRecommendation> Recommend([FromBody] BuildRequest request)
    {
        try { return Ok(advisor.GenerateBuild(request)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("builds/evaluate")]
    public ActionResult<BuildRecommendation> Evaluate([FromBody] ComputerBuild build)
    {
        if (build is null) return BadRequest(new { message = "A build is required." });
        return Ok(advisor.EvaluateBuild(build));
    }
}
