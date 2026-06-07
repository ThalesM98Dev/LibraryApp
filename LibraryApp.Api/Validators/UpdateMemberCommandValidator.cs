using FluentValidation;
using LibraryApp.Core.DTOs;

namespace LibraryApp.Api.Validators;

public class UpdateMemberCommandValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberCommandValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(150).WithMessage("Full name must be 150 characters or fewer.")
            .Matches(@"^[a-zA-Z\s\-'.]+$").WithMessage("Full name can only contain letters, spaces, hyphens, apostrophes, and periods.")
            .When(x => x.FullName is not null);

        RuleFor(x => x.Email)
            .MaximumLength(200).WithMessage("Email must be 200 characters or fewer.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .When(x => x.Email is not null);

        RuleFor(x => x.Status)
            .Must(s => s is "Active" or "Suspended" or "Expired")
            .WithMessage("Status must be 'Active', 'Suspended', or 'Expired'.")
            .When(x => x.Status is not null);
    }
}
