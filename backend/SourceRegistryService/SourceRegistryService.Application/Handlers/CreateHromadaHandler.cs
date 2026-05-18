using MediatR;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Application.Handlers;

public class CreateHromadaHandler : IRequestHandler<CreateHromadaCommand, Guid>
{
    private readonly IHromadaRepository _hromadas;
    private readonly IOblastRepository _oblasts;
    private readonly ICurrentUserContext _user;

    public CreateHromadaHandler(IHromadaRepository hromadas, IOblastRepository oblasts, ICurrentUserContext user)
    {
        _hromadas = hromadas;
        _oblasts = oblasts;
        _user = user;
    }

    public async Task<Guid> Handle(CreateHromadaCommand request, CancellationToken ct)
    {
        var oblast = await _oblasts.GetByIdAsync(request.OblastId)
            ?? throw new KeyNotFoundException($"Oblast {request.OblastId} not found");

        var existing = await _hromadas.GetBySlugAndOblastAsync(request.Slug, request.OblastId);
        if (existing != null)
            throw new InvalidOperationException($"Hromada with slug '{request.Slug}' already exists in this oblast");

        var hromada = new Hromada
        {
            Id = Guid.NewGuid(),
            OblastId = request.OblastId,
            Name = request.Name,
            NameUk = request.NameUk,
            Slug = request.Slug,
            CreatedAt = DateTime.UtcNow
        };

        await _hromadas.AddAsync(hromada);
        return hromada.Id;
    }
}
