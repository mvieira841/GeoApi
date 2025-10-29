using GeoApi.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoApi.Access.Persistence.EntityMappings;

public class CountryMapping : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("newsequentialid()");

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.IsoCode).HasMaxLength(3).IsRequired();

        builder.HasIndex(c => c.Name)
               .IsUnique()
               .IsDescending()
               .IsClustered(false);

        builder.HasIndex(c => c.IsoCode).IsUnique();

        builder.HasMany(c => c.Cities)
               .WithOne(city => city.Country)
               .HasForeignKey(city => city.CountryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}