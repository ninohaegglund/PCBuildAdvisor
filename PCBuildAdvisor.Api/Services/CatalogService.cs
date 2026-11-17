using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;
using PCBuildAdvisor.Api.Repositories;

namespace PCBuildAdvisor.Api.Services;

public class CatalogService(BuildRepository repository)
{
    public Task<IReadOnlyList<Game>> GetGamesAsync(CancellationToken cancellationToken = default) =>
        repository.GetGamesAsync(cancellationToken);

    public IReadOnlyList<PcComponent> GetComponents(ComponentType? type = null) => repository.GetComponents(type);
}
