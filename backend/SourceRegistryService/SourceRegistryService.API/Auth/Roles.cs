namespace SourceRegistryService.API.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";
    public const string ServiceAccount = "ServiceAccount";

    // Admins only — ServiceAccount must NOT access this service.
    public const string HumanAdmins = "Admin,SuperAdmin";
}
