using MediatR;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Commands;

public class CreateDataSourceCommand : IRequest<CreateDataSourceResult>
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string Url { get; set; } = "";
    public string Description { get; set; } = "";
    public DataSourceOrigin Origin { get; set; } = DataSourceOrigin.Manual;
    public DataSourceStatus Status { get; set; } = DataSourceStatus.Active;
}

public enum CreateDataSourceOutcome { Created, Duplicate }
public class CreateDataSourceResult
{
    public CreateDataSourceOutcome Outcome { get; set; }
    public Guid Id { get; set; }
}
