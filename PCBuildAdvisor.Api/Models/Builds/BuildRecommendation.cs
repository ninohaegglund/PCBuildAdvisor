namespace PCBuildAdvisor.Api.Models.Builds;

public class BuildRecommendation
{
    public ComputerBuild Build { get; set; } = new();
    public int CompatibilityScore { get; set; }
    public List<string> Feedback { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
