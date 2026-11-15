namespace PCBuildAdvisor.Api.Models.Builds;

public class BuildRecommendation
{
    public ComputerBuild Build { get; set; } = new();
    /// <summary>Whether mandatory component slots contain the expected component categories; compatibility is assessed separately.</summary>
    public bool IsComplete { get; set; }
    public decimal? EstimatedTotal { get; set; }
    public int CompatibilityScore { get; set; }
    public List<string> Feedback { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> UnverifiedChecks { get; set; } = [];
}
