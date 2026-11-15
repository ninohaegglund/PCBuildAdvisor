using PCBuildAdvisor.Api.DTOs;
using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;

namespace PCBuildAdvisor.Api.Services;

// Exhaustive selection is suitable for the current small sample catalog.
// A larger catalog will need a bounded search rather than enumerating every combination.
internal static class BuildSelector
{
    public static BuildRecommendation Select(IReadOnlyList<PcComponent> catalog, BuildRequest request,
        string targetTier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gaming = request.Purpose is null or BuildPurpose.Gaming;
        var needsGpu = gaming || request.Purpose is BuildPurpose.Streaming or BuildPurpose.ContentCreation or BuildPurpose.AiMl
            || !string.IsNullOrWhiteSpace(request.GpuPreference);
        var groups = catalog.ToLookup(part => part.Type);
        var gpuOptions = needsGpu
            ? groups[ComponentType.Gpu].Where(part => MatchesPreference(part, request.GpuPreference)).Cast<PcComponent?>()
            : new PcComponent?[] { null };

        var builds =
            from cpu in groups[ComponentType.Cpu].Where(part => MatchesPreference(part, request.CpuPreference))
            from board in groups[ComponentType.Motherboard]
            where KnownMatch(cpu.Socket, board.Socket)
            from ram in groups[ComponentType.Memory]
            where KnownMatch(ram.MemoryType, board.MemoryType) ||
                board.SupportedMemoryTypes.Any(type => KnownMatch(type, ram.MemoryType))
            from pcCase in groups[ComponentType.Case]
            where pcCase.SupportedFormFactors.Count > 0
                ? pcCase.SupportedFormFactors.Any(form => KnownMatch(form, board.FormFactor))
                : KnownMatch(pcCase.FormFactor, board.FormFactor)
            from gpu in gpuOptions
            from storage in groups[ComponentType.Storage]
            from psu in groups[ComponentType.PowerSupply]
            from cooler in groups[ComponentType.CpuCooler]
            select new ComputerBuild
            {
                Name = gaming ? "Game-optimized recommendation" : $"{request.Purpose} recommendation",
                Mode = request.Mode,
                Purpose = request.Purpose,
                TargetResolution = request.TargetResolution,
                Cpu = cpu, Gpu = gpu, Motherboard = board, Memory = [ram], Storage = [storage],
                PowerSupply = psu, Case = pcCase, CpuCooler = cooler
            };

        var candidates = new List<BuildRecommendation>();
        foreach (var build in builds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evaluation = BuildCompatibilityEvaluator.Evaluate(build);
            if (evaluation.IsComplete && evaluation.Warnings.Count == 0 && evaluation.EstimatedTotal.HasValue)
                candidates.Add(evaluation);
        }

        if (candidates.Count == 0)
            throw new BuildSelectionException("The current catalog cannot provide a complete, priced build without known conflicts for these preferences.");

        var targetRank = TierRank(targetTier);
        var result = candidates
            .Where(candidate => !request.MaximumEstimatedBudget.HasValue ||
                candidate.EstimatedTotal!.Value <= request.MaximumEstimatedBudget.Value)
            .OrderBy(candidate => TierShortfall(candidate.Build, targetRank))
            .ThenBy(candidate => candidate.EstimatedTotal)
            .ThenByDescending(candidate => TierRank(candidate.Build.Cpu!.PerformanceTier) +
                (candidate.Build.Gpu is null ? 0 : TierRank(candidate.Build.Gpu.PerformanceTier)))
            .FirstOrDefault();

        if (result is null)
            throw new BuildSelectionException(
                "No complete build fits this budget and the selected manufacturer preferences in the current sample catalog.",
                candidates.Min(candidate => candidate.EstimatedTotal!.Value));

        if (TierShortfall(result.Build, targetRank) > 0)
            result.Warnings.Add($"The budget and available catalog require components below the requested {targetTier} tier; the performance target is not guaranteed.");
        return result;
    }

    private static bool MatchesPreference(PcComponent part, string? preference) =>
        string.IsNullOrWhiteSpace(preference) || KnownMatch(part.Manufacturer, preference);

    private static bool KnownMatch(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static int TierShortfall(ComputerBuild build, int target) =>
        Math.Max(0, target - TierRank(build.Cpu!.PerformanceTier)) +
        (build.Gpu is null ? 0 : Math.Max(0, target - TierRank(build.Gpu.PerformanceTier)));

    private static int TierRank(string? tier) => tier?.ToLowerInvariant() switch
    {
        "entry" or "low" => 0,
        "high" => 2,
        _ => 1
    };
}
