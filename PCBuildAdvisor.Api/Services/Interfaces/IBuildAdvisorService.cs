using PCBuildAdvisor.Api.DTOs;
using PCBuildAdvisor.Api.Models.Builds;

namespace PCBuildAdvisor.Api.Services.Interfaces;

public interface IBuildAdvisorService
{
    BuildRecommendation GenerateBuild(BuildRequest request);
    BuildRecommendation EvaluateBuild(ComputerBuild build);
}
