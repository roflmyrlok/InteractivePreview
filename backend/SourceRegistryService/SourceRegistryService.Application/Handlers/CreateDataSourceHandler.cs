using MediatR;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Application.Handlers;

public class CreateDataSourceHandler : IRequestHandler<CreateDataSourceCommand, CreateDataSourceResult>
{
    private readonly IDataSourceRepository _sources;
    private readonly ICurrentUserContext _user;

    public CreateDataSourceHandler(IDataSourceRepository sources, ICurrentUserContext user)
    {
        _sources = sources;
        _user = user;
    }

    public async Task<CreateDataSourceResult> Handle(CreateDataSourceCommand request, CancellationToken ct)
    {
        var exists = await _sources.ExistsByUrlAndScopeAsync(request.Url, request.ScopeType, request.ScopeId);
        if (exists)
            return new CreateDataSourceResult { Outcome = CreateDataSourceOutcome.Duplicate };

        var source = new DataSource
        {
            Id = Guid.NewGuid(),
            ScopeType = request.ScopeType,
            ScopeId = request.ScopeId,
            Url = request.Url,
            Description = request.Description,
            Status = request.Status,
            Origin = request.Origin,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _user.UserId
        };

        await _sources.AddAsync(source);
        return new CreateDataSourceResult { Outcome = CreateDataSourceOutcome.Created, Id = source.Id };
    }
}
