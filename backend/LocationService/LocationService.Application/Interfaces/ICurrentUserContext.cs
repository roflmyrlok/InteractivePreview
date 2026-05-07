namespace LocationService.Application.Interfaces
{
    // Read-only view of the authenticated principal making the current request.
    // Implemented in the API layer using IHttpContextAccessor + JWT claims.
    public interface ICurrentUserContext
    {
        // The authenticated user's id. null when no user (background tasks) — handlers
        // that mutate must check this and reject if null.
        Guid? UserId { get; }

        // Role claim value as it appears on the JWT (e.g. "SuperAdmin", "ServiceAccount").
        string? Role { get; }

        bool IsAuthenticated { get; }
    }
}
