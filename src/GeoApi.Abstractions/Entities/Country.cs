namespace GeoApi.Abstractions.Entities;

public class Country : BaseEntity
{
    public required string Name { get; set; } = string.Empty;
    public required string IsoCode { get; set; } = string.Empty; // e.g., "US", "BR"
    public ICollection<City> Cities { get; set; } = [];
}
