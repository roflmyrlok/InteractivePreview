using FluentValidation;
using LocationService.Application.Commands;

namespace LocationService.Application.Validators
{
    public class UpdateLocationValidator : AbstractValidator<UpdateLocationCommand>
    {
        public UpdateLocationValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required")
                .MaximumLength(200);
            RuleForEach(x => x.Details).SetValidator(new CreateLocationDetailValidator());
        }
    }

    // PATCH allows partial fields — Address may be null, Details may be empty.
    // PropertyValue may be empty (means "remove this detail").
    public class PatchLocationValidator : AbstractValidator<PatchLocationCommand>
    {
        public PatchLocationValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Address).MaximumLength(200);

            RuleForEach(x => x.Details).ChildRules(d =>
            {
                d.RuleFor(x => x.PropertyName).NotEmpty().MaximumLength(50);
                d.RuleFor(x => x.PropertyValue).MaximumLength(500); // empty allowed = removal
            });
        }
    }

    public class DeleteLocationValidator : AbstractValidator<DeleteLocationCommand>
    {
        public DeleteLocationValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
