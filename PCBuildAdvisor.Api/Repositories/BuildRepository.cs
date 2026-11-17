using Microsoft.EntityFrameworkCore;
using PCBuildAdvisor.Api.Data;
using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;

namespace PCBuildAdvisor.Api.Repositories;

/// <summary>Reads games and components from the advisor database.</summary>
public class BuildRepository(AdvisorDbContext db)
{
    public async Task<IReadOnlyList<Game>> GetGamesAsync(CancellationToken cancellationToken = default) =>
        await db.Games.AsNoTracking().OrderBy(game => game.Name).ThenBy(game => game.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Game>> GetGamesByIdsAsync(IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var selectedIds = ids.Distinct().ToArray();
        return await db.Games.AsNoTracking().Where(game => selectedIds.Contains(game.Id))
            .OrderBy(game => game.Name).ThenBy(game => game.Id).ToListAsync(cancellationToken);
    }

    public IReadOnlyList<PcComponent> GetComponents(ComponentType? type = null)
    {
        var query = db.Components.AsQueryable();
        if (type.HasValue) query = query.Where(component => component.Type == type.Value);
        return query.OrderBy(component => component.Type).ThenBy(component => component.Name).ToList();
    }
}
