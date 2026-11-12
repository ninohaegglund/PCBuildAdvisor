using PCBuildAdvisor.Api.Enums;

namespace PCBuildAdvisor.Api.Models.Builds;

public class UsageProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BuildPurpose Purpose { get; set; }
    public string? Description { get; set; }
}
