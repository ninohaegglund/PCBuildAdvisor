using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PCBuildAdvisor.Api.Data.AdvisorDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AdvisorDatabase")));

builder.Services.AddSingleton<PCBuildAdvisor.Api.Repositories.BuildRepository>();
builder.Services.AddScoped<PCBuildAdvisor.Api.Services.CatalogService>();
builder.Services.AddScoped<PCBuildAdvisor.Api.Services.Interfaces.IBuildAdvisorService,
    PCBuildAdvisor.Api.Services.BuildAdvisorService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PCBuildAdvisor.Api.Data.AdvisorDbContext>();
    await db.Database.MigrateAsync();
    await PCBuildAdvisor.Api.Data.AdvisorDbSeeder.SeedGamesAsync(db);
}

app.Run();
