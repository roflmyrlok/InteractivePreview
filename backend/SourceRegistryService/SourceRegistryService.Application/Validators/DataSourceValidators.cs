using FluentValidation;
using SourceRegistryService.Application.Commands;

namespace SourceRegistryService.Application.Validators;

public class CreateDataSourceValidator : AbstractValidator<CreateDataSourceCommand>
{
    public CreateDataSourceValidator()
    {
        RuleFor(x => x.ScopeId).NotEmpty();
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(1000)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == "https" || uri.Scheme == "http"))
            .WithMessage("Url must be a valid absolute HTTP/HTTPS URL")
            .Must(url =>
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
                return uri.Host.EndsWith(".gov.ua", StringComparison.OrdinalIgnoreCase);
            })
            .WithMessage("Url host must be a .gov.ua domain");
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class PatchDataSourceValidator : AbstractValidator<PatchDataSourceCommand>
{
    public PatchDataSourceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}
