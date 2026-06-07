using FluentValidation;
using LibraryApp.Core.DTOs;

namespace LibraryApp.Api.Validators;

public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    private static readonly HashSet<string> ValidConditions =
        new() { "New", "Good", "Fair", "Poor" };

    public CreateBookCommandValidator()
    {
        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required.")
            .MaximumLength(20).WithMessage("ISBN must be 20 characters or fewer.")
            .Matches(@"^[0-9\-X]+$").WithMessage("ISBN may only contain digits, hyphens, and X.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must be 300 characters or fewer.");

        RuleFor(x => x.PublicationYear)
            .InclusiveBetween(1000, DateTime.UtcNow.Year)
            .WithMessage($"Publication year must be between 1000 and {DateTime.UtcNow.Year}.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description must be 4000 characters or fewer.")
            .When(x => x.Description is not null);

        RuleFor(x => x.AuthorIds)
            .NotEmpty().WithMessage("At least one author is required.");

        RuleFor(x => x.AuthorIds)
            .Must(ids => ids.All(id => id > 0))
            .WithMessage("All author IDs must be positive integers.")
            .When(x => x.AuthorIds is not null);

        RuleFor(x => x.CategoryIds)
            .NotEmpty().WithMessage("At least one category is required.");

        RuleFor(x => x.CategoryIds)
            .Must(ids => ids.All(id => id > 0))
            .WithMessage("All category IDs must be positive integers.")
            .When(x => x.CategoryIds is not null);

        RuleFor(x => x.InitialCopies)
            .InclusiveBetween(1, 100)
            .WithMessage("Initial copies must be between 1 and 100.");

        RuleFor(x => x.CopyCondition)
            .NotEmpty().WithMessage("Copy condition is required.");

        RuleFor(x => x.CopyCondition)
            .Must(c => ValidConditions.Contains(c))
            .WithMessage("Condition must be New, Good, Fair, or Poor.")
            .When(x => x.CopyCondition is not null);
    }
}