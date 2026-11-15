using PCBuildAdvisor.Api.DTOs;
using PCBuildAdvisor.Api.Models.Builds;

namespace PCBuildAdvisor.Api.Services.Interfaces;

public interface IBuildAdvisorService
{
    Task<BuildRecommendation> GenerateBuildAsync(BuildRequest request, CancellationToken cancellationToken = default);
    BuildRecommendation EvaluateBuild(ComputerBuild build);
}
