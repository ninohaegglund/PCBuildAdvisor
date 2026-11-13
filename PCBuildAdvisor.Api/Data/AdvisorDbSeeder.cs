using Microsoft.EntityFrameworkCore;
using PCBuildAdvisor.Api.Models.Builds;

namespace PCBuildAdvisor.Api.Data;

public static class AdvisorDbSeeder
{
    public static async Task SeedGamesAsync(AdvisorDbContext db)
    {
        var games = new Game[]
        {
            new() { Slug = "apex", Name = "Apex Legends", Genre = "Battle royale", Demand = "balanced", ImageUrl = "https://upload.wikimedia.org/wikipedia/en/d/db/Apex_legends_cover.jpg" },
            new() { Slug = "warzone", Name = "Call of Duty: Warzone", Genre = "Competitive FPS", Demand = "demanding", ImageUrl = "https://upload.wikimedia.org/wikipedia/en/6/65/COD_Warzone_Cover_Art.jpg" },
            new() { Slug = "cyberpunk", Name = "Cyberpunk 2077", Genre = "Action RPG", Demand = "demanding", ImageUrl = "https://upload.wikimedia.org/wikipedia/en/9/9f/Cyberpunk_2077_box_art.jpg" },
            new() { Slug = "elden-ring", Name = "Elden Ring", Genre = "Action RPG", Demand = "demanding", ImageUrl = "https://i.ebayimg.com/images/g/9owAAOSww4RiKBzU/s-l1200.jpg" },
            new() { Slug = "valorant", Name = "Valorant", Genre = "Competitive FPS", Demand = "light", ImageUrl = "https://m.media-amazon.com/images/M/MV5BZmQwMjQ2ZTUtZmM5MC00MTdkLWIxYzgtODU1NzQ4Zjg4NmMxXkEyXkFqcGc@._V1_.jpg" },
            new() { Slug = "baldurs-gate", Name = "Baldur's Gate 3", Genre = "RPG", Demand = "balanced", ImageUrl = "https://live-production.wcms.abc-cdn.net.au/6fb825b2cc3e8a5e1f90fb65ca6a57ee?impolicy=wcms_crop_resize&cropH=2160&cropW=2160&xPos=40&yPos=0&width=862&height=862" },
            new() { Slug = "forza", Name = "Forza Horizon 6", Genre = "Racing", Demand = "balanced", ImageUrl = "https://content.presspage.com/uploads/1523/cca058d2-d82a-4d74-94a5-4bdc6f4da17f/1920_fh6_evergreen_keyart_branded-square_2160x2160_rgb.png?10000" },
            new() { Slug = "minecraft", Name = "Minecraft", Genre = "Sandbox", Demand = "light", ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ7udRVuulbP8UqXqsn-S_RyGYlmx3T-l5UPOXOhNiKzw&s=10" }
        };

        var existingSlugs = await db.Games.Select(game => game.Slug).ToListAsync();
        db.Games.AddRange(games.Where(game => !existingSlugs.Contains(game.Slug)));
        await db.SaveChangesAsync();
    }
}
