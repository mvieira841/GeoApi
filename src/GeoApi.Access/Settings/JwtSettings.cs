using System.ComponentModel.DataAnnotations;

namespace GeoApi.Access.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Key { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public int DurationInHours { get; set; } = 1;
}