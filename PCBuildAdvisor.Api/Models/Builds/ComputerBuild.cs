using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Components;

namespace PCBuildAdvisor.Api.Models.Builds;

public class ComputerBuild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BuildMode Mode { get; set; }
    public BuildPurpose? Purpose { get; set; }
    public TargetResolution? TargetResolution { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Game> Games { get; set; } = [];

    public PcComponent? Cpu { get; set; }
    public PcComponent? Gpu { get; set; }
    public PcComponent? Motherboard { get; set; }
    public List<PcComponent> Memory { get; set; } = [];
    public List<PcComponent> Storage { get; set; } = [];
    public PcComponent? PowerSupply { get; set; }
    public PcComponent? Case { get; set; }
    public PcComponent? CpuCooler { get; set; }
}
