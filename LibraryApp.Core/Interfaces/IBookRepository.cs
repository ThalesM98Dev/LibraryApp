
namespace LibraryApp.Core.Interfaces;

public interface IBookRepository
{
    Task<PagedResult<BookDto>> SearchAsync(BookSearchQuery query, CancellationToken ct = default);
    Task<BookDto?> GetByIdAsync(int bookId, CancellationToken ct = default);
    Task<AvailableCopyDto?> GetAvailableCopyAsync(int bookId, CancellationToken ct = default);
    Task<bool> IsbnExistsAsync(string isbn, CancellationToken ct = default);
    Task<CreateBookResult> AddAsync(              // replaces void Add(Book)
           CreateBookCommand command,
           CancellationToken ct = default);

    Task<TrackedBookDto?> GetTrackedAsync(int bookId, CancellationToken ct = default);
    Task UpdateAsync(int bookId, UpdateBookCommand command, CancellationToken ct = default);

    Task DeleteAsync(int bookId, CancellationToken ct = default);

    Task DeleteCopiesByBookIdAsync(int bookId, CancellationToken ct = default);
}
