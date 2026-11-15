namespace PCBuildAdvisor.Api.Services;

public class BuildSelectionException(string message, decimal? minimumEstimatedTotal = null) : Exception(message)
{
    public decimal? MinimumEstimatedTotal { get; } = minimumEstimatedTotal;
}
