using PCBuildAdvisor.Api.DTOs;
using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;
using PCBuildAdvisor.Api.Repositories;
using PCBuildAdvisor.Api.Services.Interfaces;

namespace PCBuildAdvisor.Api.Services;

public class BuildAdvisorService(BuildRepository repository) : IBuildAdvisorService
{
    public BuildRecommendation GenerateBuild(BuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MaximumEstimatedBudget is < 0)
            throw new ArgumentException("Budget cannot be negative.", nameof(request));

        var games = repository.GetGamesByIds(request.GameIds);
        var tier = GetRecommendedTier(request, games);
        var gaming = request.Purpose is null or BuildPurpose.Gaming;

        var preferredSocket = request.CpuPreference?.Contains("Intel", StringComparison.OrdinalIgnoreCase) == true
            ? "LGA1700"
            : gaming ? "AM5" : null;
        var cpu = Choose(ComponentType.Cpu, tier, request.CpuPreference, preferredSocket);
        var gpu = gaming || request.Purpose is BuildPurpose.ContentCreation or BuildPurpose.AiMl
            ? Choose(ComponentType.Gpu, tier, request.GpuPreference)
            : null;
        var motherboard = Choose(ComponentType.Motherboard, tier, socket: cpu?.Socket);
        var memory = motherboard?.MemoryType is { } ramType
            ? repository.GetComponents(ComponentType.Memory).FirstOrDefault(x => x.MemoryType == ramType)
            : null;
        var storage = Choose(ComponentType.Storage, tier);
        var psu = Choose(ComponentType.PowerSupply, tier,
            minimumWattage: (cpu?.PowerConsumptionWatts ?? 0) + (gpu?.PowerConsumptionWatts ?? 0) + 250);
        var pcCase = Choose(ComponentType.Case, tier, form: motherboard?.FormFactor);
        var cooler = Choose(ComponentType.CpuCooler, tier);

        var build = new ComputerBuild
        {
            Name = gaming ? "Game-optimized recommendation" : $"{request.Purpose} recommendation",
            Mode = request.Mode,
            Purpose = request.Purpose,
            TargetResolution = request.TargetResolution,
            Games = games.ToList(),
            Cpu = cpu,
            Gpu = gpu,
            Motherboard = motherboard,
            Memory = memory is null ? [] : [memory],
            Storage = storage is null ? [] : [storage],
            PowerSupply = psu,
            Case = pcCase,
            CpuCooler = cooler
        };

        var result = EvaluateBuild(build);
        result.Feedback.Insert(0, $"Starter recommendation based on the selected use case and a {tier} component tier.");
        if (games.Count != request.GameIds.Distinct().Count())
            result.Warnings.Add("One or more selected game IDs were not found in the current sample catalog.");
        if (request.MaximumEstimatedBudget.HasValue && GetEstimatedTotal(build) > request.MaximumEstimatedBudget.Value)
            result.Warnings.Add($"Estimated total ({GetEstimatedTotal(build):C0}) exceeds the entered budget ({request.MaximumEstimatedBudget.Value:C0}). Prices are illustrative sample estimates.");
        result.Feedback.Add($"Estimated component total: {GetEstimatedTotal(build):C0}. These are manually maintained sample estimates, not live prices.");
        return result;
    }

    public BuildRecommendation EvaluateBuild(ComputerBuild build)
    {
        ArgumentNullException.ThrowIfNull(build);
        var warnings = new List<string>();
        var feedback = new List<string>();
        var parts = GetParts(build).ToList();

        if (parts.Count == 0)
            warnings.Add("Add components to the build to receive compatibility feedback.");
        if (build.Cpu is not null && build.Motherboard is not null &&
            build.Cpu.Socket is not null && build.Motherboard.Socket is not null &&
            !Same(build.Cpu.Socket, build.Motherboard.Socket))
            warnings.Add($"CPU socket {build.Cpu.Socket} does not match motherboard socket {build.Motherboard.Socket}.");

        if (build.Motherboard is not null && build.Memory.Count > 0 && build.Motherboard.MemoryType is not null)
        {
            foreach (var ram in build.Memory.Where(x => x.MemoryType is not null && !Same(x.MemoryType, build.Motherboard.MemoryType)))
                warnings.Add($"{ram.Name} uses {ram.MemoryType}, but the motherboard supports {build.Motherboard.MemoryType}.");
        }

        if (build.Motherboard is not null && build.Case is not null &&
            build.Motherboard.FormFactor is not null && build.Case.SupportedFormFactors.Count > 0 &&
            !build.Case.SupportedFormFactors.Any(x => Same(x, build.Motherboard.FormFactor)))
            warnings.Add($"The case may not support the {build.Motherboard.FormFactor} motherboard form factor.");

        if (build.Gpu is not null && build.Case is not null && build.Gpu.LengthMm.HasValue &&
            TrySpec(build.Case, "GpuClearanceMm", out var gpuClearance) && build.Gpu.LengthMm > gpuClearance)
            warnings.Add($"GPU length ({build.Gpu.LengthMm} mm) exceeds the case clearance ({gpuClearance} mm).");

        if (build.CpuCooler is not null && build.Case is not null &&
            TrySpec(build.CpuCooler, "CoolerHeightMm", out var coolerHeight) &&
            TrySpec(build.Case, "CoolerClearanceMm", out var coolerClearance) && coolerHeight > coolerClearance)
            warnings.Add($"CPU cooler height ({coolerHeight} mm) exceeds the case clearance ({coolerClearance} mm).");

        var requiredWatts = parts.Sum(x => x.PowerConsumptionWatts ?? 0);
        if (build.PowerSupply?.Wattage is int psuWatts && requiredWatts > 0 && psuWatts < requiredWatts * 1.25)
            warnings.Add($"The PSU may have too little headroom: selected parts list about {requiredWatts} W, while the PSU is {psuWatts} W.");

        if (warnings.Count == 0)
            feedback.Add(parts.Count == 0 ? "No compatibility assessment is available yet." : "No conflicts were found in the compatibility data currently available.");
        else
            feedback.Add("Review the listed issues before buying. Checks are limited to the specifications present in this sample catalog.");

        var checkedCount = new[] { build.Cpu, build.Gpu, build.Motherboard, build.PowerSupply, build.Case, build.CpuCooler }
            .Count(x => x is not null) + build.Memory.Count;
        return new BuildRecommendation
        {
            Build = build,
            CompatibilityScore = checkedCount == 0 ? 0 : Math.Max(0, 100 - warnings.Count * 25),
            Feedback = feedback,
            Warnings = warnings
        };
    }

    private PcComponent? Choose(ComponentType type, string tier, string? brand = null,
        string? socket = null, string? form = null, int minimumWattage = 0)
    {
        var candidates = repository.GetComponents(type).AsEnumerable();
        if (brand is not null) candidates = candidates.Where(x => x.Manufacturer.Contains(brand, StringComparison.OrdinalIgnoreCase));
        if (socket is not null) candidates = candidates.Where(x => x.Socket is null || Same(x.Socket, socket));
        if (form is not null) candidates = candidates.Where(x => x.FormFactor is null || Same(x.FormFactor, form) ||
            x.SupportedFormFactors.Any(s => Same(s, form)));
        if (minimumWattage > 0) candidates = candidates.Where(x => (x.Wattage ?? 0) >= minimumWattage);
        return candidates.OrderByDescending(x => TierRank(x.PerformanceTier) <= TierRank(tier))
            .ThenBy(x => Math.Abs(TierRank(x.PerformanceTier) - TierRank(tier)))
            .ThenBy(x => x.EstimatedPrice)
            .FirstOrDefault();
    }

    private static string GetRecommendedTier(BuildRequest request, IReadOnlyList<Game> games)
    {
        if (request.Purpose is BuildPurpose.OfficeWork) return "entry";
        if (request.TargetResolution == TargetResolution.UltraHd4K ||
            games.Any(x => Same(x.Demand, "demanding")) || Same(request.PerformanceGoal, "high")) return "high";
        if (games.Any(x => Same(x.Demand, "light")) && request.TargetResolution == TargetResolution.FullHd1080p &&
            Same(request.PerformanceGoal, "value")) return "entry";
        return "mid";
    }

    private static decimal GetEstimatedTotal(ComputerBuild build) => GetParts(build).Sum(x => x.EstimatedPrice ?? 0);
    private static IEnumerable<PcComponent> GetParts(ComputerBuild build) =>
        new[] { build.Cpu, build.Gpu, build.Motherboard, build.PowerSupply, build.Case, build.CpuCooler }
            .Where(x => x is not null).Cast<PcComponent>().Concat(build.Memory).Concat(build.Storage);
    private static int TierRank(string? tier) => tier?.ToLowerInvariant() switch { "entry" or "low" => 0, "high" => 2, _ => 1 };
    private static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static bool TrySpec(PcComponent component, string key, out int value)
    {
        value = 0;
        return component.Specifications.TryGetValue(key, out var text) && int.TryParse(text, out value);
    }
}
