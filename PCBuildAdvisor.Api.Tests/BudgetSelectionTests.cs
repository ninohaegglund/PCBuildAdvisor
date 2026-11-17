using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCBuildAdvisor.Api.Controllers;
using PCBuildAdvisor.Api.Data;
using PCBuildAdvisor.Api.DTOs;
using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Repositories;
using PCBuildAdvisor.Api.Services;
using Xunit;

namespace PCBuildAdvisor.Api.Tests;

public class BudgetSelectionTests
{
    [Fact]
    public async Task HigherTierPartDoesNotLoseToAMoreExpensiveLowerTierBuild()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        var unrestricted = await advisor.GenerateBuildAsync(new());
        var amdOnly = await advisor.GenerateBuildAsync(new() { CpuPreference = "AMD" });

        Assert.True(unrestricted.EstimatedTotal <= amdOnly.EstimatedTotal);
        Assert.Equal("high", unrestricted.Build.Cpu!.PerformanceTier);
    }

    [Fact]
    public async Task CancelledRecommendationDoesNotReturnABuild()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            advisor.GenerateBuildAsync(new(), new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task LowerBudgetChangesPartsWhilePreservingACompleteBuild()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        var generous = await advisor.GenerateBuildAsync(new() { PerformanceGoal = "high", MaximumEstimatedBudget = 2000 });
        var limited = await advisor.GenerateBuildAsync(new() { PerformanceGoal = "high", MaximumEstimatedBudget = 1200 });

        Assert.True(generous.IsComplete);
        Assert.True(limited.IsComplete);
        Assert.InRange(limited.EstimatedTotal!.Value, 1, 1200);
        Assert.True(generous.EstimatedTotal > limited.EstimatedTotal);
        Assert.NotEqual(generous.Build.Gpu!.Id, limited.Build.Gpu!.Id);
        Assert.Empty(advisor.EvaluateBuild(limited.Build).Warnings);
        Assert.Contains(limited.Warnings, warning => warning.Contains("below the requested high tier"));
    }

    [Fact]
    public async Task ExactMinimumBudgetWorksButOneCentLessDoesNot()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));
        var impossible = await Assert.ThrowsAsync<BuildSelectionException>(() =>
            advisor.GenerateBuildAsync(new() { MaximumEstimatedBudget = 1 }));
        Assert.NotNull(impossible.MinimumEstimatedTotal);

        var minimum = impossible.MinimumEstimatedTotal.Value;
        var result = await advisor.GenerateBuildAsync(new() { MaximumEstimatedBudget = minimum });
        Assert.True(result.IsComplete);
        Assert.Equal(minimum, result.EstimatedTotal);
        await Assert.ThrowsAsync<BuildSelectionException>(() =>
            advisor.GenerateBuildAsync(new() { MaximumEstimatedBudget = minimum - 0.01m }));
    }

    [Fact]
    public async Task ImpossibleBudgetReturns422WithMinimumCost()
    {
        await using var db = CreateDatabase();
        var repository = new BuildRepository(db);
        var controller = new BuildController(new BuildAdvisorService(repository), new CatalogService(repository));

        var response = await controller.Recommend(new() { MaximumEstimatedBudget = 0 }, CancellationToken.None);

        var error = Assert.IsType<UnprocessableEntityObjectResult>(response.Result);
        var body = System.Text.Json.JsonSerializer.SerializeToElement(error.Value);
        Assert.True(body.GetProperty("minimumEstimatedTotal").GetDecimal() > 0);
        Assert.Contains("No complete build fits", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task BudgetCannotSilentlyOverrideManufacturerPreferences()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        var result = await advisor.GenerateBuildAsync(new()
        {
            CpuPreference = "AMD", GpuPreference = "AMD", MaximumEstimatedBudget = 2000
        });
        Assert.Equal("AMD", result.Build.Cpu!.Manufacturer);
        Assert.Equal("AMD", result.Build.Gpu!.Manufacturer);
        await Assert.ThrowsAsync<BuildSelectionException>(() => advisor.GenerateBuildAsync(new()
        {
            CpuPreference = "AMD", GpuPreference = "AMD", MaximumEstimatedBudget = 1200
        }));
    }

    [Fact]
    public async Task SelectionUsesEvaluationForPsuHeadroom()
    {
        await using var db = CreateDatabase();
        var repository = new BuildRepository(db);
        // Simulate a catalog correction without changing the shared sample source.
        foreach (var psu in repository.GetComponents(ComponentType.PowerSupply)) psu.Wattage = 50;

        var error = await Assert.ThrowsAsync<BuildSelectionException>(() =>
            new BuildAdvisorService(repository).GenerateBuildAsync(new()));

        Assert.Null(error.MinimumEstimatedTotal);
    }

    [Fact]
    public async Task MissingPricesCannotMakeABuildAppearAffordable()
    {
        await using var db = CreateDatabase();
        var repository = new BuildRepository(db);
        foreach (var storage in repository.GetComponents(ComponentType.Storage)) storage.EstimatedPrice = null;

        await Assert.ThrowsAsync<BuildSelectionException>(() =>
            new BuildAdvisorService(repository).GenerateBuildAsync(new() { MaximumEstimatedBudget = 2000 }));
    }

    [Fact]
    public async Task StreamingAndExplicitGpuPreferencesIncludeAGpu()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        Assert.NotNull((await advisor.GenerateBuildAsync(new() { Purpose = BuildPurpose.Streaming })).Build.Gpu);
        var office = await advisor.GenerateBuildAsync(new() { Purpose = BuildPurpose.OfficeWork, GpuPreference = "NVIDIA" });
        Assert.Equal("NVIDIA", office.Build.Gpu!.Manufacturer);
    }

    [Fact]
    public async Task MixedGamesDoNotDowngradeToEntryTierBecauseOneGameIsLight()
    {
        await using var db = CreateDatabase();
        db.Games.AddRange(new Game { Id = 41, Name = "Light", Slug = "light", Demand = "light" },
            new Game { Id = 42, Name = "Balanced", Slug = "balanced", Demand = "balanced" });
        await db.SaveChangesAsync();
        var repository = new BuildRepository(db);
        // Supply an entry option to make a wrongly reduced target observable in the selected parts.
        var gpu = repository.GetComponents(ComponentType.Gpu)[0];
        gpu.PerformanceTier = "entry";

        var result = await new BuildAdvisorService(repository).GenerateBuildAsync(new()
        {
            GameIds = [41, 42], TargetResolution = TargetResolution.FullHd1080p, PerformanceGoal = "value"
        });

        Assert.NotEqual("entry", result.Build.Gpu!.PerformanceTier);
        Assert.Equal(2, result.Build.Games.Count);
    }

    private static AdvisorDbContext CreateDatabase()
    {
        var db = new AdvisorDbContext(
            new DbContextOptionsBuilder<AdvisorDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        AdvisorDbSeeder.SeedComponentsAsync(db).GetAwaiter().GetResult();
        return db;
    }
}
