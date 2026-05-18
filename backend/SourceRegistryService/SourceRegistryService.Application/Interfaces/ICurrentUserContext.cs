namespace SourceRegistryService.Application.Interfaces;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
