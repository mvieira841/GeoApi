namespace GeoApi.Host.Endpoints.Constants;

public static class CountriesEndpointsConstants
{
    public const string ResourceName = "Countries";

    public static class EndpointNames
    {
        public const string GetAll = "GetAllCountries";
        public const string GetById = "GetCountryById";
        public const string Create = "CreateCountry";
        public const string Update = "UpdateCountry";
        public const string Delete = "DeleteCountry";
    }

    public static class Paths
    {
        public const string Main = "/countries";
        public const string GetAll = "/";
        public const string GetById = "/{id:guid}";
        public const string Create = "/";
        public const string Update = "/{id:guid}";
        public const string Delete = "/{id:guid}";
    }

    public static class SummaryDescriptions
    {
        public const string Main = "Endpoints for managing countries.";
        public const string GetAll = "Get all countries.";
        public const string GetById = "Get a country by its ID.";
        public const string Create = "Create a new country.";
        public const string Update = "Update an existing country.";
        public const string Delete = "Delete a country by its ID.";
    }
}
