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
        ValidateRequest(request);

        var components = repository.GetComponents();
        ValidatePreference(components, ComponentType.Cpu, request.CpuPreference);
        ValidatePreference(components, ComponentType.Gpu, request.GpuPreference);

        var games = await repository.GetGamesByIdsAsync(request.GameIds, cancellationToken);
        if (games.Count != request.GameIds.Distinct().Count())
            throw new ArgumentException("One or more selected game IDs were not found in the catalog.", nameof(request));
        var tier = GetRecommendedTier(request, games);
        var result = BuildSelector.Select(components, request, tier, cancellationToken);
        result.Build.Games = games.ToList();
        result.Feedback.Insert(0, $"Starter recommendation targeting the {tier} CPU/GPU tier for the selected use case.");
        result.Feedback.Add($"Estimated component total: {result.EstimatedTotal:0.00}. These are manually maintained sample estimates, not live prices.");
        return result;
    }

    public BuildRecommendation EvaluateBuild(ComputerBuild build) => BuildCompatibilityEvaluator.Evaluate(build);

    private void ValidateRequest(BuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MaximumEstimatedBudget is < 0)
            throw new ArgumentException("Budget cannot be negative.", nameof(request));
        if (!Enum.IsDefined(request.Mode) ||
            (request.Purpose.HasValue && !Enum.IsDefined(request.Purpose.Value)) ||
            (request.TargetResolution.HasValue && !Enum.IsDefined(request.TargetResolution.Value)))
            throw new ArgumentException("The build mode, purpose, or resolution is invalid.", nameof(request));
        if (request.GameIds is null || request.GameIds.Any(id => id <= 0))
            throw new ArgumentException("Game IDs must be a list of positive identifiers.", nameof(request));
        if (!new[] { "value", "balanced", "high" }.Any(goal => Same(goal, request.PerformanceGoal)))
            throw new ArgumentException("Performance goal must be value, balanced, or high.", nameof(request));

    }

    private static void ValidatePreference(IReadOnlyList<PcComponent> components, ComponentType type,
        string? preference)
    {
        if (!string.IsNullOrWhiteSpace(preference) &&
            !components.Any(part => part.Type == type && Same(part.Manufacturer, preference)))
            throw new ArgumentException($"The {type} manufacturer preference is not available in the current catalog.");
    }

    private static string GetRecommendedTier(BuildRequest request, IReadOnlyList<Game> games)
    {
        if (request.Purpose is BuildPurpose.OfficeWork) return "entry";
        if (request.TargetResolution == TargetResolution.UltraHd4K ||
            games.Any(x => Same(x.Demand, "demanding")) || Same(request.PerformanceGoal, "high")) return "high";
        if (games.Count > 0 && games.All(x => Same(x.Demand, "light")) && request.TargetResolution == TargetResolution.FullHd1080p &&
            Same(request.PerformanceGoal, "value")) return "entry";
        return "mid";
    }

    private static bool Same(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}
