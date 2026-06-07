using FluentValidation;
using LibraryApp.Core.DTOs;

namespace LibraryApp.Api.Validators;

public class CreateAuthorCommandValidator : AbstractValidator<CreateAuthorCommand>
{
    public CreateAuthorCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Author name is required.")
            .MaximumLength(150).WithMessage("Author name must be 150 characters or fewer.")
            .Matches(@"^[a-zA-Z\s\-'.]+$").WithMessage("Author name can only contain letters, spaces, hyphens, apostrophes, and periods.");

        RuleFor(x => x.Biography)
            .MaximumLength(4000).WithMessage("Biography must be 4000 characters or fewer.")
            .When(x => x.Biography is not null);
    }
}