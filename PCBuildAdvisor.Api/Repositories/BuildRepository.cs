using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;

namespace PCBuildAdvisor.Api.Repositories;

/// <summary>Temporary in-memory catalog. Replace with database queries when persistence is added.</summary>
public class BuildRepository
{
    private readonly List<Game> _games =
    [
        new() { Id = 1, Name = "Apex Legends", Genre = "Battle royale", Demand = "balanced" },
        new() { Id = 2, Name = "Call of Duty: Warzone", Genre = "Competitive FPS", Demand = "demanding" },
        new() { Id = 3, Name = "Cyberpunk 2077", Genre = "Action RPG", Demand = "demanding" },
        new() { Id = 4, Name = "Elden Ring", Genre = "Action RPG", Demand = "demanding" },
        new() { Id = 5, Name = "Valorant", Genre = "Competitive FPS", Demand = "light" },
        new() { Id = 6, Name = "Baldur's Gate 3", Genre = "RPG", Demand = "balanced" },
        new() { Id = 7, Name = "Forza Horizon 6", Genre = "Racing", Demand = "balanced" },
        new() { Id = 8, Name = "Minecraft", Genre = "Sandbox", Demand = "light" }
    ];

    private readonly List<PcComponent> _components =
    [
        Part(1, ComponentType.Cpu, "Ryzen 5 7600", "AMD", "AM5", price: 199, tier: "mid", watts: 65),
        Part(2, ComponentType.Cpu, "Ryzen 7 7800X3D", "AMD", "AM5", price: 359, tier: "high", watts: 120),
        Part(3, ComponentType.Cpu, "Core i5-14600K", "Intel", "LGA1700", price: 289, tier: "high", watts: 125),
        Part(4, ComponentType.Gpu, "GeForce RTX 4060", "NVIDIA", price: 299, tier: "mid", watts: 115, length: 245),
        Part(5, ComponentType.Gpu, "GeForce RTX 4070 SUPER", "NVIDIA", price: 599, tier: "high", watts: 220, length: 305),
        Part(6, ComponentType.Gpu, "Radeon RX 7800 XT", "AMD", price: 489, tier: "high", watts: 263, length: 302),
        Part(7, ComponentType.Motherboard, "B650 AORUS Elite AX", "Gigabyte", "AM5", "DDR5", "ATX", 189),
        Part(8, ComponentType.Motherboard, "B760M Pro RS", "ASRock", "LGA1700", "DDR5", "Micro ATX", 139),
        Part(9, ComponentType.Motherboard, "B550M Steel Legend", "ASRock", "AM4", "DDR4", "Micro ATX", 119),
        Part(10, ComponentType.Memory, "Vengeance 32 GB DDR5", "Corsair", memory: "DDR5", price: 109, capacity: 32),
        Part(11, ComponentType.Memory, "Ripjaws V 32 GB DDR4", "G.Skill", memory: "DDR4", price: 69, capacity: 32),
        Part(12, ComponentType.Storage, "990 EVO 1 TB", "Samsung", price: 89, capacity: 1000),
        Part(13, ComponentType.PowerSupply, "RM650e", "Corsair", price: 89, tier: "mid", wattage: 650),
        Part(14, ComponentType.PowerSupply, "RM850x SHIFT", "Corsair", price: 149, tier: "high", wattage: 850),
        Part(15, ComponentType.Case, "North Airflow", "Fractal Design", form: "ATX", price: 139, gpuClearance: 355, coolerClearance: 170),
        Part(16, ComponentType.Case, "AP201 Mesh", "ASUS", form: "Micro ATX", price: 89, gpuClearance: 338, coolerClearance: 170),
        Part(17, ComponentType.CpuCooler, "Peerless Assassin 120", "Thermalright", price: 44, coolerHeight: 157),
        Part(18, ComponentType.CpuCooler, "Pure Rock 2", "be quiet!", price: 39, coolerHeight: 155)
    ];

    public IReadOnlyList<Game> GetGames() => _games;
    public IReadOnlyList<PcComponent> GetComponents(ComponentType? type = null) =>
        type.HasValue ? _components.Where(x => x.Type == type).ToList() : _components;
    public IReadOnlyList<Game> GetGamesByIds(IEnumerable<int> ids) =>
        _games.Where(x => ids.Contains(x.Id)).ToList();

    private static PcComponent Part(int id, ComponentType type, string name, string brand,
        string? socket = null, string? memory = null, string? form = null, decimal price = 0,
        string tier = "mid", int watts = 0, int wattage = 0, int length = 0,
        int capacity = 0, int gpuClearance = 0, int coolerClearance = 0, int coolerHeight = 0) =>
        new()
        {
            Id = id, Type = type, Name = name, Manufacturer = brand, ModelNumber = name,
            Socket = socket, MemoryType = memory, FormFactor = form, EstimatedPrice = price,
            PerformanceTier = tier, PowerConsumptionWatts = watts, Wattage = wattage,
            LengthMm = type == ComponentType.Gpu ? length : null,
            CapacityGb = type == ComponentType.Storage ? capacity : null,
            MemoryCapacityGb = type == ComponentType.Memory ? capacity : null,
            Specifications = new Dictionary<string, string>
            {
                ["GpuClearanceMm"] = gpuClearance.ToString(),
                ["CoolerClearanceMm"] = coolerClearance.ToString(),
                ["CoolerHeightMm"] = coolerHeight.ToString()
            }
        };
}
