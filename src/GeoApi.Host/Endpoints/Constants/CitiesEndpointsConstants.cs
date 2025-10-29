namespace GeoApi.Host.Endpoints.Constants;

public static class CitiesEndpointsConstants
{
    public const string ResourceName = "Cities";

    public static class EndpointNames
    {
        public const string GetAll = "GetAllCities";
        public const string GetById = "GetCityById";
        public const string Create = "CreateCity";
        public const string Update = "UpdateCity";
        public const string Delete = "DeleteCity";
    }

    public static class Paths
    {
        public const string Main = "/countries/{countryId:guid}/cities";
        public const string GetAll = "/";
        public const string GetById = "/{id:guid}";
        public const string Create = "/";
        public const string Update = "/{id:guid}";
        public const string Delete = "/{id:guid}";
    }

    public static class SummaryDescriptions
    {
        public const string Main = "Endpoints for managing cities within a country.";
        public const string GetAllByCountry = "Get all cities for a specific country.";
        public const string GetById = "Get a specific city by its ID.";
        public const string Create = "Create a new city within a specific country.";
        public const string Update = "Update an existing city's details.";
        public const string Delete = "Delete a city by its ID.";
    }
}