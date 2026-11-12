using PCBuildAdvisor.Api.Enums;

namespace PCBuildAdvisor.Api.DTOs;

public class BuildRequest
{
    public BuildMode Mode { get; set; }
    public BuildPurpose? Purpose { get; set; }
    public TargetResolution? TargetResolution { get; set; }
    public List<int> GameIds { get; set; } = [];
    public string? CpuPreference { get; set; }
    public string? GpuPreference { get; set; }
}
