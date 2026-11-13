using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PCBuildAdvisor.Api.Models.Components;

namespace PCBuildAdvisor.Api.Data.Configurations;

public class ComponentConfiguration : IEntityTypeConfiguration<PcComponent>
{
    public void Configure(EntityTypeBuilder<PcComponent> builder)
    {
        builder.ToTable("Components");
        builder.HasKey(component => component.Id);
        builder.Property(component => component.Name).HasMaxLength(200).IsRequired();
        builder.Property(component => component.Manufacturer).HasMaxLength(100).IsRequired();
        builder.Property(component => component.ModelNumber).HasMaxLength(100).IsRequired();
        builder.Property(component => component.Description).HasMaxLength(1000);
        builder.Property(component => component.EstimatedPrice).HasPrecision(12, 2);
        builder.Property(component => component.PerformanceTier).HasMaxLength(30).IsRequired();

        var converter = new ValueConverter<Dictionary<string, string>, string>(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<Dictionary<string, string>>(value, (JsonSerializerOptions?)null) ?? new());
        var comparer = new ValueComparer<Dictionary<string, string>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
            value => JsonSerializer.Deserialize<Dictionary<string, string>>(
                JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new());
        builder.Property(component => component.Specifications).HasConversion(converter).Metadata.SetValueComparer(comparer);

        builder.HasIndex(component => component.Type);
        builder.HasIndex(component => component.ModelNumber);
    }
}
