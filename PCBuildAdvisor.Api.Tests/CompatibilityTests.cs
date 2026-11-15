using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;
using PCBuildAdvisor.Api.Services;
using Xunit;

namespace PCBuildAdvisor.Api.Tests;

public class CompatibilityTests
{
    [Fact]
    public void CpuOnlyBuildIsIncompleteAndCannotReceivePerfectScore()
    {
        var result = BuildCompatibilityEvaluator.Evaluate(new ComputerBuild { Cpu = Part(ComponentType.Cpu) });

        Assert.False(result.IsComplete);
        Assert.True(result.CompatibilityScore < 100);
        Assert.Contains(result.Warnings, warning => warning.Contains("storage"));
        Assert.Contains(result.Warnings, warning => warning.Contains("motherboard"));
    }

    [Fact]
    public void EmptyBuildHasZeroScore()
    {
        var result = BuildCompatibilityEvaluator.Evaluate(new ComputerBuild());

        Assert.False(result.IsComplete);
        Assert.Equal(0, result.CompatibilityScore);
    }

    [Fact]
    public void CompleteBuildReturnsNumericTotal()
    {
        var result = BuildCompatibilityEvaluator.Evaluate(CompleteBuild());

        Assert.True(result.IsComplete);
        Assert.Empty(result.Warnings);
        Assert.Equal(800m, result.EstimatedTotal);
    }

    [Fact]
    public void UnknownPriceDoesNotBecomeZeroCost()
    {
        var build = CompleteBuild();
        build.Gpu!.EstimatedPrice = null;

        Assert.Null(BuildCompatibilityEvaluator.Evaluate(build).EstimatedTotal);
    }

    [Fact]
    public void OverflowingComponentTotalIsRejectedAsInvalidInput()
    {
        var build = CompleteBuild();
        build.Cpu!.EstimatedPrice = decimal.MaxValue;

        var exception = Assert.Throws<ArgumentException>(() => BuildCompatibilityEvaluator.Evaluate(build));

        Assert.Contains("component total", exception.Message);
    }

    [Fact]
    public void UnknownSocketIsUnverifiedRatherThanPerfectCompatibility()
    {
        var build = CompleteBuild();
        build.Cpu!.Socket = null;

        var result = BuildCompatibilityEvaluator.Evaluate(build);

        Assert.True(result.CompatibilityScore < 100);
        Assert.Contains(result.UnverifiedChecks, check => check.Contains("socket compatibility"));
    }

    [Fact]
    public void WrongComponentTypeCannotFillARequiredSlot()
    {
        var build = CompleteBuild();
        build.Cpu!.Type = ComponentType.Gpu;

        var result = BuildCompatibilityEvaluator.Evaluate(build);

        Assert.False(result.IsComplete);
        Assert.Contains(result.Warnings, warning => warning.Contains("CPU slot contains a Gpu"));
    }

    [Fact]
    public void AtxBoardInMicroAtxCaseIsFlaggedWithoutSupportedFormFactors()
    {
        var build = CompleteBuild();
        build.Case!.FormFactor = "Micro ATX";
        build.Case.SupportedFormFactors.Clear();

        var result = BuildCompatibilityEvaluator.Evaluate(build);

        Assert.Contains(result.Warnings, warning => warning.Contains("ATX motherboard exceeds"));
    }

    [Fact]
    public void ExplicitCaseSupportAllowsSmallerMotherboard()
    {
        var build = CompleteBuild();
        build.Motherboard!.FormFactor = "Micro ATX";
        build.Case!.SupportedFormFactors = ["ATX", "Micro ATX"];

        Assert.Empty(BuildCompatibilityEvaluator.Evaluate(build).Warnings);
    }

    [Fact]
    public void UndocumentedCaseSupportRemainsUnverified()
    {
        var build = CompleteBuild();
        build.Motherboard!.FormFactor = "Micro ATX";
        build.Case!.SupportedFormFactors.Clear();

        var result = BuildCompatibilityEvaluator.Evaluate(build);

        Assert.Contains(result.UnverifiedChecks, check => check.Contains("case has no list"));
    }

    [Fact]
    public void UnsupportedCoolerSocketIsFlagged()
    {
        var build = CompleteBuild();
        build.CpuCooler!.SupportedSockets = ["LGA1700"];

        Assert.Contains(BuildCompatibilityEvaluator.Evaluate(build).Warnings,
            warning => warning.Contains("cooler does not list support"));
    }

    [Fact]
    public void MemoryCapacityAboveMotherboardLimitIsFlagged()
    {
        var build = CompleteBuild();
        build.Motherboard!.MaxMemoryCapacityGb = 16;

        Assert.Contains(BuildCompatibilityEvaluator.Evaluate(build).Warnings,
            warning => warning.Contains("Installed memory (32 GB) exceeds"));
    }

    [Fact]
    public void MissingDiscreteGpuRequiresGraphicsVerification()
    {
        var build = CompleteBuild();
        build.Gpu = null;

        var result = BuildCompatibilityEvaluator.Evaluate(build);

        Assert.True(result.IsComplete);
        Assert.Contains(result.UnverifiedChecks, check => check.Contains("integrated graphics"));
        Assert.True(result.CompatibilityScore < 100);
    }

    [Theory]
    [InlineData("socket", "does not match motherboard socket")]
    [InlineData("memory", "motherboard supports")]
    [InlineData("gpu", "GPU length")]
    [InlineData("cooler", "CPU cooler height")]
    [InlineData("psu", "too little headroom")]
    public void ExistingCompatibilityConflictsRemainDetected(string conflict, string message)
    {
        var build = CompleteBuild();
        switch (conflict)
        {
            case "socket": build.Cpu!.Socket = "LGA1700"; break;
            case "memory": build.Memory[0].MemoryType = "DDR4"; break;
            case "gpu": build.Gpu!.LengthMm = 400; break;
            case "cooler": build.CpuCooler!.HeightMm = 200; break;
            case "psu": build.PowerSupply!.Wattage = 50; break;
        }

        Assert.Contains(BuildCompatibilityEvaluator.Evaluate(build).Warnings, warning => warning.Contains(message));
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("storage")]
    [InlineData("games")]
    [InlineData("specifications")]
    [InlineData("supportedSockets")]
    public void NullCollectionsAreRejected(string collection)
    {
        var build = CompleteBuild();
        switch (collection)
        {
            case "memory": build.Memory = null!; break;
            case "storage": build.Storage = null!; break;
            case "games": build.Games = null!; break;
            case "specifications": build.Cpu!.Specifications = null!; break;
            case "supportedSockets": build.CpuCooler!.SupportedSockets = null!; break;
        }

        Assert.Throws<ArgumentException>(() => BuildCompatibilityEvaluator.Evaluate(build));
    }

    [Fact]
    public void NullCollectionItemsAreRejected()
    {
        var build = CompleteBuild();
        build.Memory.Add(null!);

        Assert.Throws<ArgumentException>(() => BuildCompatibilityEvaluator.Evaluate(build));
    }

    [Fact]
    public void NegativePowerCannotHideInsufficientPsuCapacity()
    {
        var build = CompleteBuild();
        build.Gpu!.PowerConsumptionWatts = -1000;

        Assert.Throws<ArgumentException>(() => BuildCompatibilityEvaluator.Evaluate(build));
    }

    private static PcComponent Part(ComponentType type) => new()
    {
        Name = type.ToString(), Type = type, EstimatedPrice = 100m, PowerConsumptionWatts = 10
    };

    private static ComputerBuild CompleteBuild()
    {
        var build = new ComputerBuild
        {
            Cpu = Part(ComponentType.Cpu), Gpu = Part(ComponentType.Gpu),
            Motherboard = Part(ComponentType.Motherboard), PowerSupply = Part(ComponentType.PowerSupply),
            Case = Part(ComponentType.Case), CpuCooler = Part(ComponentType.CpuCooler),
            Memory = [Part(ComponentType.Memory)], Storage = [Part(ComponentType.Storage)]
        };
        build.Cpu.Socket = "AM5";
        build.Motherboard.Socket = "AM5";
        build.Motherboard.MemoryType = "DDR5";
        build.Motherboard.MaxMemoryCapacityGb = 128;
        build.Motherboard.FormFactor = "ATX";
        build.Memory[0].MemoryType = "DDR5";
        build.Memory[0].MemoryCapacityGb = 32;
        build.Gpu.LengthMm = 200;
        build.Case.FormFactor = "ATX";
        build.Case.SupportedFormFactors = ["ATX"];
        build.Case.Specifications = new() { ["GpuClearanceMm"] = "300", ["CoolerClearanceMm"] = "170" };
        build.CpuCooler.HeightMm = 150;
        build.CpuCooler.SupportedSockets = ["AM5"];
        build.PowerSupply.Wattage = 650;
        return build;
    }
}
