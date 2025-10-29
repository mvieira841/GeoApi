namespace GeoApi.Host.Endpoints.Constants;

public static class AuthEndpointsConstants
{
    public const string ResourceName = "Auth";

    public static class EndpointNames
    {
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string Register = "Register";
        public const string RefreshToken = "RefreshToken";
        public const string ChangePassword = "ChangePassword";
        public const string ForgotPassword = "ForgotPassword";
        public const string ResetPassword = "ResetPassword";
    }

    public static class Paths
    {
        public const string Main = "/auth";
        public const string Login = "/login";
        public const string Logout = "/logout";
        public const string Register = "/register";
        public const string RefreshToken = "/refresh";
        public const string ChangePassword = "/change-password";
        public const string ForgotPassword = "/forgot-password";
        public const string ResetPassword = "/reset-password";
    }

    public static class SummaryDescriptions
    {
        public const string Main = "Endpoints for managing cities within a country.";
        public const string Login = "Authenticate a user and return a JWT token.";
        public const string Logout = "Log out the current user.";
        public const string Register = "Register a new user.";
        public const string Refresh = "Refresh the JWT token.";
        public const string ChangePassword = "Change the password for the current user.";
        public const string ForgotPassword = "Initiate the password reset process.";
        public const string ResetPassword = "Reset the user's password using a token.";
    }
}
