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

public class RequestValidationTests
{
    [Fact]
    public async Task NegativeBudgetIsRejected()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        await Assert.ThrowsAnyAsync<ArgumentException>(() => advisor.GenerateBuildAsync(
            new BuildRequest { MaximumEstimatedBudget = -1 }));
    }

    [Theory]
    [InlineData("mode")]
    [InlineData("purpose")]
    [InlineData("resolution")]
    public async Task UndefinedRecommendationEnumsAreRejected(string field)
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));
        var request = new BuildRequest();
        switch (field)
        {
            case "mode": request.Mode = (BuildMode)999; break;
            case "purpose": request.Purpose = (BuildPurpose)999; break;
            case "resolution": request.TargetResolution = (TargetResolution)999; break;
        }

        await Assert.ThrowsAnyAsync<ArgumentException>(() => advisor.GenerateBuildAsync(request));
    }

    [Fact]
    public async Task NullGameSelectionIsRejected()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        await Assert.ThrowsAnyAsync<ArgumentException>(() => advisor.GenerateBuildAsync(
            new BuildRequest { GameIds = null! }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(73)]
    public async Task InvalidOrUnknownGameIdsAreRejected(int gameId)
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        await Assert.ThrowsAnyAsync<ArgumentException>(() => advisor.GenerateBuildAsync(
            new BuildRequest { GameIds = [gameId] }));
    }

    [Fact]
    public async Task RepeatedValidGameIdsPreserveTheDatabaseGameOnce()
    {
        await using var db = CreateDatabase();
        db.Games.Add(new Game { Id = 73, Slug = "selected", Name = "Selected game", Demand = "demanding" });
        await db.SaveChangesAsync();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        var result = await advisor.GenerateBuildAsync(new BuildRequest { GameIds = [73, 73] });

        var game = Assert.Single(result.Build.Games);
        Assert.Equal(73, game.Id);
        Assert.Equal("selected", game.Slug);
        Assert.Equal("Selected game", game.Name);
        Assert.Equal("demanding", game.Demand);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fast")]
    public async Task MissingOrUnsupportedPerformanceGoalsAreRejected(string? goal)
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        await Assert.ThrowsAnyAsync<ArgumentException>(() => advisor.GenerateBuildAsync(
            new BuildRequest { PerformanceGoal = goal! }));
    }

    [Theory]
    [InlineData("value", " VALUE ")]
    [InlineData("balanced", " Balanced ")]
    [InlineData("high", " HIGH ")]
    public async Task PerformanceGoalWhitespaceAndCaseDoNotChangeTheSelection(string goal, string normalizedGoal)
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        var expected = await advisor.GenerateBuildAsync(new BuildRequest { PerformanceGoal = goal });
        var actual = await advisor.GenerateBuildAsync(new BuildRequest { PerformanceGoal = normalizedGoal });

        Assert.NotNull(actual.Build.Cpu);
        Assert.NotNull(actual.Build.Gpu);
        Assert.Equal(expected.Build.Cpu?.Id, actual.Build.Cpu.Id);
        Assert.Equal(expected.Build.Gpu?.Id, actual.Build.Gpu.Id);
    }

    [Theory]
    [InlineData("Int", null)]
    [InlineData("Unknown", null)]
    [InlineData(null, "Nvid")]
    [InlineData(null, "Intel")]
    public async Task UnsupportedOrPartialManufacturerNamesAreRejected(string? cpuPreference, string? gpuPreference)
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        await Assert.ThrowsAnyAsync<ArgumentException>(() => advisor.GenerateBuildAsync(
            new BuildRequest { CpuPreference = cpuPreference, GpuPreference = gpuPreference }));
    }

    [Fact]
    public async Task ManufacturerPreferencesIgnoreWhitespaceAndCase()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        var result = await advisor.GenerateBuildAsync(new BuildRequest
        {
            CpuPreference = " intel ", GpuPreference = " nvidia "
        });

        Assert.Equal("Intel", result.Build.Cpu?.Manufacturer);
        Assert.Equal("NVIDIA", result.Build.Gpu?.Manufacturer);
    }

    [Fact]
    public async Task BlankManufacturerPreferencesAllowNormalSelection()
    {
        await using var db = CreateDatabase();
        var advisor = new BuildAdvisorService(new BuildRepository(db));

        var result = await advisor.GenerateBuildAsync(new BuildRequest
        {
            CpuPreference = "  ", GpuPreference = ""
        });

        Assert.NotNull(result.Build.Cpu);
        Assert.NotNull(result.Build.Gpu);
    }

    [Fact]
    public void ComponentsEndpointRejectsUndefinedComponentType()
    {
        using var db = CreateDatabase();
        var controller = CreateController(db);

        Assert.IsType<BadRequestObjectResult>(controller.GetComponents((ComponentType)999));
    }

    [Fact]
    public async Task RecommendationEndpointReturnsBadRequestForInvalidPreferences()
    {
        await using var db = CreateDatabase();
        var controller = CreateController(db);

        var response = await controller.Recommend(new BuildRequest { CpuPreference = "Int" }, default);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("storage")]
    [InlineData("games")]
    public void EvaluationEndpointReturnsBadRequestForNullCollectionItems(string collection)
    {
        using var db = CreateDatabase();
        var controller = CreateController(db);
        var build = new ComputerBuild();
        switch (collection)
        {
            case "memory": build.Memory = [null!]; break;
            case "storage": build.Storage = [null!]; break;
            case "games": build.Games = [null!]; break;
        }

        Assert.IsType<BadRequestObjectResult>(controller.Evaluate(build).Result);
    }

    private static BuildController CreateController(AdvisorDbContext db)
    {
        var repository = new BuildRepository(db);
        return new BuildController(new BuildAdvisorService(repository), new CatalogService(repository));
    }

    private static AdvisorDbContext CreateDatabase()
    {
        var db = new AdvisorDbContext(
            new DbContextOptionsBuilder<AdvisorDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        AdvisorDbSeeder.SeedComponentsAsync(db).GetAwaiter().GetResult();
        return db;
    }
}
