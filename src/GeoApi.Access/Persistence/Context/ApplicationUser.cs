using Microsoft.AspNetCore.Identity;

namespace GeoApi.Access.Persistence.Context;

public class ApplicationUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}