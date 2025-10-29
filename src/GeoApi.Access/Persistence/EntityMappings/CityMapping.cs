using GeoApi.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoApi.Access.Persistence.EntityMappings;

public class CityMapping : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("newsequentialid()");

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();

        builder.Property(c => c.Latitude).HasPrecision(9, 6);
        builder.Property(c => c.Longitude).HasPrecision(9, 6);

        builder.HasIndex(c => new { c.Name, c.CountryId }).IsUnique();

        builder.HasIndex(c => c.Name)
               .IsDescending()
               .IsClustered(false);
    }
}