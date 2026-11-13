using Microsoft.EntityFrameworkCore;
using PCBuildAdvisor.Api.Data;
using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;
using PCBuildAdvisor.Api.Repositories;

namespace PCBuildAdvisor.Api.Services;

public class CatalogService(AdvisorDbContext db, BuildRepository repository)
{
    public async Task<IReadOnlyList<Game>> GetGamesAsync() =>
        await db.Games.AsNoTracking().OrderBy(game => game.Name).ToListAsync();

    // Component catalog remains sample data in memory until its database seed is added.
    public IReadOnlyList<PcComponent> GetComponents(ComponentType? type = null) => repository.GetComponents(type);
}
