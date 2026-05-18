using FluentValidation;
using SourceRegistryService.Application.Commands;

namespace SourceRegistryService.Application.Validators;

public class CreateHromadaValidator : AbstractValidator<CreateHromadaCommand>
{
    public CreateHromadaValidator()
    {
        RuleFor(x => x.OblastId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameUk).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, digits and hyphens");
    }
}

public class PatchHromadaValidator : AbstractValidator<PatchHromadaCommand>
{
    public PatchHromadaValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name != null);
        RuleFor(x => x.NameUk).MaximumLength(200).When(x => x.NameUk != null);
        RuleFor(x => x.Slug).MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, digits and hyphens")
            .When(x => x.Slug != null);
    }
}

public class DeleteHromadaValidator : AbstractValidator<DeleteHromadaCommand>
{
    public DeleteHromadaValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
