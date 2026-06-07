using FluentValidation;
using LibraryApp.Core.DTOs;

namespace LibraryApp.Api.Validators;

public class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    private static readonly HashSet<string> ValidConditions =
        new() { "New", "Good", "Fair", "Poor" };

    public UpdateBookCommandValidator()
    {
        // ISBN – optional, but if provided must be valid
        RuleFor(x => x.ISBN)
            .MaximumLength(20).WithMessage("ISBN must be 20 characters or fewer.")
            .Matches(@"^[0-9\-X]+$").WithMessage("ISBN may only contain digits, hyphens, and X.")
            .When(x => x.ISBN is not null);

        // Title – optional, but if provided must be valid
        RuleFor(x => x.Title)
            .MaximumLength(300).WithMessage("Title must be 300 characters or fewer.")
            .When(x => x.Title is not null);

        // PublicationYear – optional, but if provided must be within range
        RuleFor(x => x.PublicationYear)
            .InclusiveBetween(1000, DateTime.UtcNow.Year)
            .WithMessage($"Publication year must be between 1000 and {DateTime.UtcNow.Year}.")
            .When(x => x.PublicationYear.HasValue);

        // Description – optional, length check only if provided
        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description must be 4000 characters or fewer.")
            .When(x => x.Description is not null);

        // AuthorIds – optional, but if provided must be non‑empty and all IDs > 0
        RuleFor(x => x.AuthorIds)
            .Must(ids => ids == null || (ids.Any() && ids.All(id => id > 0)))
            .WithMessage("If author IDs are provided, the list must not be empty and all IDs must be positive integers.");

        // CategoryIds – optional, same rule as above
        RuleFor(x => x.CategoryIds)
            .Must(ids => ids == null || (ids.Any() && ids.All(id => id > 0)))
            .WithMessage("If category IDs are provided, the list must not be empty and all IDs must be positive integers.");
    }
}