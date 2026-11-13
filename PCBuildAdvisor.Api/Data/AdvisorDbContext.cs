using Microsoft.EntityFrameworkCore;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;

namespace PCBuildAdvisor.Api.Data;

public class AdvisorDbContext(DbContextOptions<AdvisorDbContext> options) : DbContext(options)
{
    public DbSet<PcComponent> Components => Set<PcComponent>();
    public DbSet<Game> Games => Set<Game>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdvisorDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
