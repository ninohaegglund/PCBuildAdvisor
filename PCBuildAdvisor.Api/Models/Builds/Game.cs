namespace PCBuildAdvisor.Api.Models.Builds;

public class Game
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public string? ImageUrl { get; set; }
    public string Demand { get; set; } = "balanced";
    public string? Description { get; set; }
}
