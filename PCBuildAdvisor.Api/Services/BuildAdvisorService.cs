using PCBuildAdvisor.Api.DTOs;
using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;
using PCBuildAdvisor.Api.Repositories;
using PCBuildAdvisor.Api.Services.Interfaces;

namespace PCBuildAdvisor.Api.Services;

public class BuildAdvisorService(BuildRepository repository) : IBuildAdvisorService
{
    public async Task<BuildRecommendation> GenerateBuildAsync(BuildRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MaximumEstimatedBudget is < 0)
            throw new ArgumentException("Budget cannot be negative.", nameof(request));

        var games = await repository.GetGamesByIdsAsync(request.GameIds, cancellationToken);
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

    public BuildRecommendation EvaluateBuild(ComputerBuild build) => BuildCompatibilityEvaluator.Evaluate(build);

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
}
