using Microsoft.EntityFrameworkCore;
using PCBuildAdvisor.Api.Data;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Repositories;
using PCBuildAdvisor.Api.Services;
using Xunit;

namespace PCBuildAdvisor.Api.Tests;

public class CatalogTests
{
    [Fact]
    public async Task BrowsedGameIdsResolveToTheSameDatabaseGames()
    {
        await using var db = CreateDatabase();
        db.Games.AddRange(
            new Game { Id = 904, Slug = "zeta", Name = "Zeta", Demand = "light" },
            new Game
            {
                Id = 73, Slug = "alpha", Name = "Alpha", Demand = "demanding",
                Genre = "Strategy", ImageUrl = "https://example.com/alpha.png",
                Description = "Catalog-specific requirements"
            });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new BuildRepository(db);
        var catalog = new CatalogService(repository);

        var browsed = await catalog.GetGamesAsync();
        var resolved = await repository.GetGamesByIdsAsync(browsed.Select(game => game.Id));

        Assert.Equal(new[] { 73, 904 }, browsed.Select(game => game.Id));
        Assert.Equal(browsed.Select(game => game.Id), resolved.Select(game => game.Id));
        var selected = Assert.Single(resolved, game => game.Id == 73);
        Assert.Equal("alpha", selected.Slug);
        Assert.Equal("Alpha", selected.Name);
        Assert.Equal("demanding", selected.Demand);
        Assert.Equal("Strategy", selected.Genre);
        Assert.Equal("https://example.com/alpha.png", selected.ImageUrl);
        Assert.Equal("Catalog-specific requirements", selected.Description);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task LookupReturnsEachSelectedGameOnceAndIgnoresMissingIds()
    {
        await using var db = CreateDatabase();
        db.Games.AddRange(
            new Game { Id = 73, Slug = "selected", Name = "Selected" },
            new Game { Id = 904, Slug = "unselected", Name = "Unselected" });
        await db.SaveChangesAsync();
        var repository = new BuildRepository(db);

        var selected = await repository.GetGamesByIdsAsync([73, 73, 1]);

        Assert.Equal(73, Assert.Single(selected).Id);
        Assert.Empty(await repository.GetGamesByIdsAsync([1]));
        Assert.Empty(await repository.GetGamesByIdsAsync([]));
    }

    private static AdvisorDbContext CreateDatabase() => new(
        new DbContextOptionsBuilder<AdvisorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
