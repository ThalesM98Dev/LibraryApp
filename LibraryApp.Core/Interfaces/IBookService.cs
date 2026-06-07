namespace LibraryApp.Core.Interfaces;

public interface IBookService
{
    Task<CreateBookResult> CreateAsync(
        CreateBookCommand command,
        CancellationToken ct = default);
    Task<BookDto> UpdateAsync(
       int bookId, UpdateBookCommand command, CancellationToken ct = default);

    Task DeleteAsync(int bookId, CancellationToken ct = default);
}