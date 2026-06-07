using FluentValidation;
using LibraryApp.Core.DTOs;

namespace LibraryApp.Api.Validators;

public class DeleteBookCommandValidator : AbstractValidator<DeleteBookCommand>
{

    public DeleteBookCommandValidator()
    {
        RuleFor(x => x.BookId)
           .NotNull().WithMessage("BookId is required.")
           .GreaterThan(0).WithMessage("BookId must be a positive integer.");
    }
}