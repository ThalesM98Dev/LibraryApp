using FluentValidation;
using LibraryApp.Core.DTOs;

namespace LibraryApp.Api.Validators;

public class CreateMemberCommandValidator : AbstractValidator<CreateMemberCommand>
{
    public CreateMemberCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(150).WithMessage("Full name must be 150 characters or fewer.")
            .Matches(@"^[a-zA-Z\s\-'.]+$").WithMessage("Full name can only contain letters, spaces, hyphens, apostrophes, and periods.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(200).WithMessage("Email must be 200 characters or fewer.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}
