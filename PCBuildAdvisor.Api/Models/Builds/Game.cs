namespace PCBuildAdvisor.Api.Models.Builds;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
