namespace GeoApi.Abstractions.Entities;

public class City : BaseEntity
{
    public required string Name { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public Guid CountryId { get; set; }
    public Country Country { get; set; } = null!;
}
