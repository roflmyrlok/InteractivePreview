namespace LocationService.API.Auth
{
    // Role names used in [Authorize(Roles = ...)] attributes.
    // Must match UserRole.ToString() values produced by UserService when issuing JWTs.
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";
        public const string ServiceAccount = "ServiceAccount";

        // Common combinations
        public const string Writers = "Admin,SuperAdmin,ServiceAccount";
        public const string HumanAdmins = "Admin,SuperAdmin";
        public const string SuperOnly = "SuperAdmin";
    }
}
