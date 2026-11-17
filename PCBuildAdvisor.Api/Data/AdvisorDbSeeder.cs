using Microsoft.EntityFrameworkCore;
using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;

namespace PCBuildAdvisor.Api.Data;

public static class AdvisorDbSeeder
{
    /// <summary>
    /// Adds the starter component catalog when its entries are missing. Existing catalog rows are
    /// left untouched so local edits and future user-managed component data are preserved.
    /// </summary>
    public static async Task SeedComponentsAsync(AdvisorDbContext db)
    {
        var components = CreateStarterComponents();
        var existingKeys = await db.Components
            .Select(component => new { component.Type, component.ModelNumber })
            .ToListAsync();
        var existing = existingKeys
            .Select(component => (component.Type, component.ModelNumber))
            .ToHashSet();
        var missing = components
            .Where(component => !existing.Contains((component.Type, component.ModelNumber)))
            .ToArray();

        if (missing.Length == 0) return;

        db.Components.AddRange(missing);
        await db.SaveChangesAsync();
    }

    public static async Task SeedGamesAsync(AdvisorDbContext db)
    {
        var games = new Game[]
        {
            new() { Slug = "apex", Name = "Apex Legends", Genre = "Battle royale", Demand = "balanced", ImageUrl = "https://upload.wikimedia.org/wikipedia/en/d/db/Apex_legends_cover.jpg" },
            new() { Slug = "warzone", Name = "Call of Duty: Warzone", Genre = "Competitive FPS", Demand = "demanding", ImageUrl = "https://upload.wikimedia.org/wikipedia/en/6/65/COD_Warzone_Cover_Art.jpg" },
            new() { Slug = "cyberpunk", Name = "Cyberpunk 2077", Genre = "Action RPG", Demand = "demanding", ImageUrl = "https://upload.wikimedia.org/wikipedia/en/9/9f/Cyberpunk_2077_box_art.jpg" },
            new() { Slug = "elden-ring", Name = "Elden Ring", Genre = "Action RPG", Demand = "demanding", ImageUrl = "https://i.ebayimg.com/images/g/9owAAOSww4RiKBzU/s-l1200.jpg" },
            new() { Slug = "valorant", Name = "Valorant", Genre = "Competitive FPS", Demand = "light", ImageUrl = "https://m.media-amazon.com/images/M/MV5BZmQwMjQ2ZTUtZmM5MC00MTdkLWIxYzgtODU1NzQ4Zjg4NmMxXkEyXkFqcGc@._V1_.jpg" },
            new() { Slug = "baldurs-gate", Name = "Baldur's Gate 3", Genre = "RPG", Demand = "balanced", ImageUrl = "https://live-production.wcms.abc-cdn.net.au/6fb825b2cc3e8a5e1f90fb65ca6a57ee?impolicy=wcms_crop_resize&cropH=2160&cropW=2160&xPos=40&yPos=0&width=862&height=862" },
            new() { Slug = "forza", Name = "Forza Horizon 6", Genre = "Racing", Demand = "balanced", ImageUrl = "https://content.presspage.com/uploads/1523/cca058d2-d82a-4d74-94a5-4bdc6f4da17f/1920_fh6_evergreen_keyart_branded-square_2160x2160_rgb.png?10000" },
            new() { Slug = "minecraft", Name = "Minecraft", Genre = "Sandbox", Demand = "light", ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ7udRVuulbP8UqXqsn-S_RyGYlmx3T-l5UPOXOhNiKzw&s=10" }
        };

        var existingSlugs = await db.Games.Select(game => game.Slug).ToListAsync();
        db.Games.AddRange(games.Where(game => !existingSlugs.Contains(game.Slug)));
        await db.SaveChangesAsync();
    }

    private static PcComponent[] CreateStarterComponents() =>
    [
        Part(ComponentType.Cpu, "Ryzen 5 7600", "AMD", 199, "mid", socket: "AM5", power: 65,
            specs: Specs(("Cores", "6 cores / 12 threads"), ("Boost", "Up to 5.1 GHz"), ("TDP", "65 W"))),
        Part(ComponentType.Cpu, "Ryzen 7 7800X3D", "AMD", 359, "high", socket: "AM5", power: 120,
            specs: Specs(("Cores", "8 cores / 16 threads"), ("Boost", "Up to 5.0 GHz"), ("TDP", "120 W"))),
        Part(ComponentType.Cpu, "Core i5-14600K", "Intel", 289, "high", socket: "LGA1700", power: 125,
            specs: Specs(("Cores", "14 cores / 20 threads"), ("Boost", "Up to 5.3 GHz"), ("TDP", "125 W"))),

        Part(ComponentType.Gpu, "GeForce RTX 4060", "NVIDIA", 299, "mid", power: 115, length: 245,
            specs: Specs(("VRAM", "8 GB GDDR6"), ("Length", "245 mm"), ("Power", "115 W"))),
        Part(ComponentType.Gpu, "GeForce RTX 4070 SUPER", "NVIDIA", 599, "high", power: 220, length: 305,
            specs: Specs(("VRAM", "12 GB GDDR6X"), ("Length", "305 mm"), ("Power", "220 W"))),
        Part(ComponentType.Gpu, "Radeon RX 7800 XT", "AMD", 489, "high", power: 263, length: 302,
            specs: Specs(("VRAM", "16 GB GDDR6"), ("Length", "302 mm"), ("Power", "263 W"))),

        Part(ComponentType.Motherboard, "B650 AORUS Elite AX", "Gigabyte", 189, "high",
            socket: "AM5", memoryType: "DDR5", formFactor: "ATX", power: 50,
            supportedMemoryTypes: ["DDR5"], memorySlots: 4, maxMemory: 192,
            specs: Specs(("Chipset", "AMD B650"), ("Networking", "Wi-Fi 6E"), ("Expansion", "PCIe 4.0"))),
        Part(ComponentType.Motherboard, "B760M Pro RS", "ASRock", 139, "mid",
            socket: "LGA1700", memoryType: "DDR5", formFactor: "Micro ATX", power: 50,
            supportedMemoryTypes: ["DDR5"], memorySlots: 4, maxMemory: 192,
            specs: Specs(("Chipset", "Intel B760"), ("Networking", "2.5 Gb LAN"), ("Expansion", "PCIe 4.0"))),
        Part(ComponentType.Motherboard, "B550M Steel Legend", "ASRock", 119, "mid",
            socket: "AM4", memoryType: "DDR4", formFactor: "Micro ATX", power: 50,
            supportedMemoryTypes: ["DDR4"], memorySlots: 4, maxMemory: 128,
            specs: Specs(("Chipset", "AMD B550"), ("Networking", "2.5 Gb LAN"), ("Expansion", "PCIe 4.0"))),

        Part(ComponentType.Memory, "Vengeance 32 GB DDR5", "Corsair", 109, "mid",
            memoryType: "DDR5", power: 8, memoryCapacity: 32,
            specs: Specs(("Capacity", "32 GB (2 × 16 GB)"), ("Speed", "DDR5-6000"), ("Latency", "CL36"))),
        Part(ComponentType.Memory, "Ripjaws V 32 GB DDR4", "G.Skill", 69, "mid",
            memoryType: "DDR4", power: 8, memoryCapacity: 32,
            specs: Specs(("Capacity", "32 GB (2 × 16 GB)"), ("Speed", "DDR4-3600"), ("Latency", "CL18"))),

        Part(ComponentType.Storage, "990 EVO 1 TB", "Samsung", 89, "high", power: 5, capacity: 1000,
            storageInterface: "NVMe PCIe 4.0",
            specs: Specs(("Capacity", "1 TB"), ("Interface", "NVMe PCIe 4.0"), ("Read", "Up to 5,000 MB/s"))),

        Part(ComponentType.PowerSupply, "RM650e", "Corsair", 89, "mid", wattage: 650,
            specs: Specs(("Efficiency", "80+ Gold"), ("Modular", "Fully modular"), ("Warranty", "7 years"))),
        Part(ComponentType.PowerSupply, "RM850x SHIFT", "Corsair", 149, "high", wattage: 850,
            specs: Specs(("Efficiency", "80+ Gold"), ("Modular", "Fully modular"), ("Warranty", "10 years"))),

        Part(ComponentType.Case, "North Airflow", "Fractal Design", 139, "high", formFactor: "ATX",
            supportedFormFactors: ["ATX", "Micro ATX", "Mini ITX"],
            specs: Specs(("Type", "Mid tower"), ("Airflow", "High airflow mesh"), ("Front I/O", "USB-C, 2 × USB-A"),
                ("GpuClearanceMm", "355"), ("CoolerClearanceMm", "170"))),
        Part(ComponentType.Case, "AP201 Mesh", "ASUS", 89, "mid", formFactor: "Micro ATX",
            supportedFormFactors: ["Micro ATX", "Mini ITX"],
            specs: Specs(("Type", "Compact tower"), ("Airflow", "Mesh panels"), ("Front I/O", "USB-C, 2 × USB-A"),
                ("GpuClearanceMm", "338"), ("CoolerClearanceMm", "170"))),

        Part(ComponentType.CpuCooler, "Peerless Assassin 120", "Thermalright", 44, "high", height: 157,
            supportedSockets: ["AM4", "AM5", "LGA1700"],
            specs: Specs(("Type", "Dual tower air"), ("CoolerHeightMm", "157"), ("Noise", "25.6 dBA"))),
        Part(ComponentType.CpuCooler, "Pure Rock 2", "be quiet!", 39, "mid", height: 155,
            supportedSockets: ["AM4", "AM5", "LGA1700"],
            specs: Specs(("Type", "Single tower air"), ("CoolerHeightMm", "155"), ("Noise", "26.8 dBA")))
    ];

    private static PcComponent Part(ComponentType type, string name, string manufacturer, decimal price,
        string tier, string? socket = null, string? memoryType = null, string? formFactor = null,
        int? power = null, int? wattage = null, int? length = null, int? height = null,
        int? capacity = null, int? memoryCapacity = null, int? memorySlots = null, int? maxMemory = null,
        string? storageInterface = null,
        List<string>? supportedSockets = null, List<string>? supportedMemoryTypes = null,
        List<string>? supportedFormFactors = null, Dictionary<string, string>? specs = null)
    {
        var modelNumber = name;
        return new PcComponent
        {
            Type = type,
            Name = name,
            Manufacturer = manufacturer,
            ModelNumber = modelNumber,
            Description = $"Starter catalog sample for {name}.",
            EstimatedPrice = price,
            PerformanceTier = tier,
            Specifications = specs ?? [],
            Socket = socket,
            MemoryType = memoryType,
            FormFactor = formFactor,
            SupportedSockets = supportedSockets ?? [],
            SupportedMemoryTypes = supportedMemoryTypes ?? [],
            SupportedFormFactors = supportedFormFactors ?? [],
            PowerConsumptionWatts = power,
            Wattage = wattage,
            LengthMm = length,
            HeightMm = height,
            CapacityGb = capacity,
            MemoryCapacityGb = memoryCapacity,
            MemorySlots = memorySlots,
            MaxMemoryCapacityGb = maxMemory,
            StorageInterface = storageInterface
        };
    }

    private static Dictionary<string, string> Specs(params (string Key, string Value)[] values) =>
        values.ToDictionary(value => value.Key, value => value.Value);
}
