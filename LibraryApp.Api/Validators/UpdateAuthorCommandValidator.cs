using FluentValidation;
using LibraryApp.Core.DTOs;

namespace LibraryApp.Api.Validators;

public class UpdateAuthorCommandValidator : AbstractValidator<UpdateAuthorCommand>
{
    public UpdateAuthorCommandValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(300).WithMessage("FullName must be 300 characters or fewer.")
            .When(x => x.FullName is not null);

        RuleFor(x => x.Biography)
            .MaximumLength(300).WithMessage("Biography must be 300 characters or fewer.")
            .When(x => x.Biography is not null);

    }
}