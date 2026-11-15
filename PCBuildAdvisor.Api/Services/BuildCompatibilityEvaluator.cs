using PCBuildAdvisor.Api.Enums;
using PCBuildAdvisor.Api.Models.Builds;
using PCBuildAdvisor.Api.Models.Components;

namespace PCBuildAdvisor.Api.Services;

public static class BuildCompatibilityEvaluator
{
    public static BuildRecommendation Evaluate(ComputerBuild build)
    {
        ArgumentNullException.ThrowIfNull(build);
        ValidateCollections(build);

        var warnings = new List<string>();
        var unverified = new List<string>();
        var slots = new (string Name, ComponentType Type, PcComponent? Part, bool Required)[]
        {
            ("CPU", ComponentType.Cpu, build.Cpu, true),
            ("GPU", ComponentType.Gpu, build.Gpu, false),
            ("motherboard", ComponentType.Motherboard, build.Motherboard, true),
            ("power supply", ComponentType.PowerSupply, build.PowerSupply, true),
            ("case", ComponentType.Case, build.Case, true),
            ("CPU cooler", ComponentType.CpuCooler, build.CpuCooler, false)
        };
        var parts = slots.Where(slot => slot.Part is not null).Select(slot => slot.Part!)
            .Concat(build.Memory).Concat(build.Storage).ToList();
        foreach (var part in parts) ValidateComponent(part);

        var complete = true;
        foreach (var slot in slots)
        {
            if (slot.Required && slot.Part is null)
            {
                warnings.Add($"A {slot.Name} is required to complete the build.");
                complete = false;
            }
            if (slot.Part is not null && slot.Part.Type != slot.Type)
            {
                warnings.Add($"The {slot.Name} slot contains a {slot.Part.Type} component.");
                if (slot.Required) complete = false;
            }
        }
        CheckCollection(build.Memory, ComponentType.Memory, "memory");
        CheckCollection(build.Storage, ComponentType.Storage, "storage");

        if (build.Cpu is { } cpu && build.Motherboard is { } board)
        {
            if (string.IsNullOrWhiteSpace(cpu.Socket) || string.IsNullOrWhiteSpace(board.Socket))
                unverified.Add("CPU and motherboard socket compatibility could not be verified.");
            else if (!Same(cpu.Socket, board.Socket))
                warnings.Add($"CPU socket {cpu.Socket} does not match motherboard socket {board.Socket}.");
        }

        if (build.Motherboard is { } motherboard && build.Memory.Count > 0)
        {
            var supportedMemory = motherboard.SupportedMemoryTypes.ToList();
            if (!string.IsNullOrWhiteSpace(motherboard.MemoryType)) supportedMemory.Add(motherboard.MemoryType);
            foreach (var ram in build.Memory)
            {
                if (string.IsNullOrWhiteSpace(ram.MemoryType) || supportedMemory.Count == 0)
                    unverified.Add($"Memory generation compatibility could not be verified for {ram.Name}.");
                else if (!supportedMemory.Any(type => Same(type, ram.MemoryType)))
                    warnings.Add($"{ram.Name} uses {ram.MemoryType}, but the motherboard supports {string.Join(", ", supportedMemory)}.");
            }

            if (motherboard.MaxMemoryCapacityGb is > 0 && build.Memory.All(ram => ram.MemoryCapacityGb is > 0))
            {
                var installedMemory = build.Memory.Sum(ram => (long)ram.MemoryCapacityGb!.Value);
                if (installedMemory > motherboard.MaxMemoryCapacityGb)
                    warnings.Add($"Installed memory ({installedMemory} GB) exceeds the motherboard limit ({motherboard.MaxMemoryCapacityGb} GB).");
            }
            else
                unverified.Add("Installed memory capacity could not be checked against the motherboard limit.");
        }

        if (build.Motherboard is { } caseBoard && build.Case is { } pcCase)
        {
            if (string.IsNullOrWhiteSpace(caseBoard.FormFactor))
                unverified.Add("Motherboard fit could not be verified because its form factor is unknown.");
            else if (pcCase.SupportedFormFactors.Count > 0)
            {
                if (!pcCase.SupportedFormFactors.Any(form => Same(form, caseBoard.FormFactor)))
                    warnings.Add($"The case does not list support for the {caseBoard.FormFactor} motherboard form factor.");
            }
            else if (!Same(pcCase.FormFactor, caseBoard.FormFactor))
            {
                if (Same(caseBoard.FormFactor, "ATX") && Same(pcCase.FormFactor, "Micro ATX"))
                    warnings.Add("The ATX motherboard exceeds the case's listed Micro ATX form factor.");
                else
                    unverified.Add("Motherboard fit could not be verified because the case has no list of supported form factors.");
            }
        }

        if (build.Gpu is { } gpu && build.Case is { } gpuCase)
        {
            if (gpu.LengthMm is > 0 && TrySpec(gpuCase, "GpuClearanceMm", out var clearance))
            {
                if (gpu.LengthMm > clearance)
                    warnings.Add($"GPU length ({gpu.LengthMm} mm) exceeds the case clearance ({clearance} mm).");
            }
            else
                unverified.Add("GPU clearance could not be verified from the available dimensions.");
        }
        if (build.Gpu is null)
            unverified.Add("No discrete GPU is selected; integrated graphics and display output support have not been verified.");

        if (build.CpuCooler is { } cooler)
        {
            if (build.Cpu is { } cooledCpu)
            {
                if (string.IsNullOrWhiteSpace(cooledCpu.Socket) || cooler.SupportedSockets.Count == 0)
                    unverified.Add("CPU cooler mounting compatibility could not be verified.");
                else if (!cooler.SupportedSockets.Any(socket => Same(socket, cooledCpu.Socket)))
                    warnings.Add($"The CPU cooler does not list support for CPU socket {cooledCpu.Socket}.");
            }
            if (build.Case is { } coolerCase)
            {
                var heightKnown = TrySpec(cooler, "CoolerHeightMm", out var height);
                if (cooler.HeightMm is > 0) { height = cooler.HeightMm.Value; heightKnown = true; }
                if (heightKnown && TrySpec(coolerCase, "CoolerClearanceMm", out var clearance))
                {
                    if (height > clearance)
                        warnings.Add($"CPU cooler height ({height} mm) exceeds the case clearance ({clearance} mm).");
                }
                else
                    unverified.Add("CPU cooler clearance could not be verified from the available dimensions.");
            }
        }
        else
            unverified.Add("No CPU cooler is selected; a suitable included cooler has not been verified.");

        var requiredWatts = parts.Sum(part => (long)(part.PowerConsumptionWatts ?? 0));
        if (build.PowerSupply is { } psu)
        {
            if (psu.Wattage is > 0 && requiredWatts > 0)
            {
                if (psu.Wattage < requiredWatts * 1.25m)
                    warnings.Add($"The PSU may have too little headroom: selected parts list about {requiredWatts} W, while the PSU is {psu.Wattage} W.");
            }
            else
                unverified.Add("PSU headroom could not be verified from the available wattage data.");
        }
        if (parts.Any(part => (part.Type is ComponentType.Cpu or ComponentType.Gpu or ComponentType.Motherboard
                or ComponentType.Memory or ComponentType.Storage) && part.PowerConsumptionWatts is not > 0))
            unverified.Add("Some selected components have unknown power consumption; the power estimate is incomplete.");

        decimal? estimatedTotal = null;
        if (parts.All(part => part.EstimatedPrice.HasValue))
        {
            try { estimatedTotal = parts.Sum(part => part.EstimatedPrice!.Value); }
            catch (OverflowException exception)
            {
                throw new ArgumentException("The component total exceeds the supported price range.", nameof(build), exception);
            }
        }
        var score = parts.Count == 0 ? 0 : Math.Max(0, 100 - warnings.Count * 25);
        if (unverified.Count > 0) score = Math.Min(score, 75);
        var feedback = new List<string>
        {
            warnings.Count > 0
                ? "Review the listed issues before buying. Compatibility checks are limited to the specifications available."
                : "No conflicts were found in the checks supported by the available specifications."
        };
        if (unverified.Count > 0) feedback.Add("Some compatibility checks remain unverified; review them before buying.");
        if (estimatedTotal is null) feedback.Add("A component total is unavailable because one or more prices are unknown.");

        return new BuildRecommendation
        {
            Build = build,
            IsComplete = complete,
            EstimatedTotal = estimatedTotal,
            CompatibilityScore = score,
            Warnings = warnings,
            UnverifiedChecks = unverified,
            Feedback = feedback
        };

        void CheckCollection(List<PcComponent> components, ComponentType expectedType, string name)
        {
            if (components.Count == 0)
            {
                warnings.Add($"At least one {name} component is required to complete the build.");
                complete = false;
            }
            foreach (var component in components.Where(component => component.Type != expectedType))
            {
                warnings.Add($"The {name} selection contains a {component.Type} component.");
                complete = false;
            }
        }
    }

    private static void ValidateCollections(ComputerBuild build)
    {
        if (build.Memory is null || build.Storage is null || build.Games is null)
            throw new ArgumentException("Build memory, storage, and games collections cannot be null.", nameof(build));
        if (build.Memory.Any(part => part is null) || build.Storage.Any(part => part is null) || build.Games.Any(game => game is null))
            throw new ArgumentException("Build collections cannot contain null entries.", nameof(build));
    }

    private static void ValidateComponent(PcComponent component)
    {
        if (component.Specifications is null || component.SupportedSockets is null ||
            component.SupportedMemoryTypes is null || component.SupportedFormFactors is null)
            throw new ArgumentException("Component specification and support collections cannot be null.");
        if (component.SupportedSockets.Any(string.IsNullOrWhiteSpace) ||
            component.SupportedMemoryTypes.Any(string.IsNullOrWhiteSpace) ||
            component.SupportedFormFactors.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Component support collections cannot contain empty entries.");
        if (!Enum.IsDefined(component.Type)) throw new ArgumentException("The component type is invalid.");
        if (component.EstimatedPrice is < 0 || component.PowerConsumptionWatts is < 0 || component.Wattage is < 0 ||
            component.LengthMm is < 0 || component.HeightMm is < 0 || component.CapacityGb is < 0 ||
            component.MemoryCapacityGb is < 0 || component.MemorySlots is < 0 || component.MaxMemoryCapacityGb is < 0)
            throw new ArgumentException("Component prices, dimensions, capacities, and power values cannot be negative.");
    }

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool TrySpec(PcComponent component, string key, out int value)
    {
        value = 0;
        return component.Specifications.TryGetValue(key, out var text) && int.TryParse(text, out value) && value > 0;
    }
}
