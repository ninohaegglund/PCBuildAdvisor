using PCBuildAdvisor.Api.Enums;

namespace PCBuildAdvisor.Api.Models.Components;

public class PcComponent
{
    public int Id { get; set; }
    public ComponentType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public string PerformanceTier { get; set; } = "mid";
    public Dictionary<string, string> Specifications { get; set; } = [];
    public string? Socket { get; set; }
    public string? MemoryType { get; set; }
    public string? FormFactor { get; set; }
    public List<string> SupportedSockets { get; set; } = [];
    public List<string> SupportedMemoryTypes { get; set; } = [];
    public List<string> SupportedFormFactors { get; set; } = [];
    public int? PowerConsumptionWatts { get; set; }
    public int? Wattage { get; set; }
    public int? LengthMm { get; set; }
    public int? HeightMm { get; set; }
    public int? CapacityGb { get; set; }
    public int? MemoryCapacityGb { get; set; }
    public int? MemorySlots { get; set; }
    public int? MaxMemoryCapacityGb { get; set; }
    public string? StorageInterface { get; set; }
}
