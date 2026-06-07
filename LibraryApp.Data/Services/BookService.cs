using LibraryApp.Core.Exceptions;
using LibraryApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApp.Data.Services
{
    public class BookService : IBookService
    {
        private readonly IUnitOfWork _uow;
        private readonly LibraryContext _ctx;
        private readonly ILogger<BookService> _logger;

        public BookService(IUnitOfWork uow, LibraryContext ctx, ILogger<BookService> logger)
        {
            _uow = uow;
            _ctx = ctx;
            _logger = logger;
        }

        public async Task<CreateBookResult> CreateAsync(
            CreateBookCommand cmd, CancellationToken ct = default)
        {
            // pre-transaction checks (unchanged)
            if (await _uow.Books.IsbnExistsAsync(cmd.ISBN, ct))
                throw new ConflictException($"A book with ISBN '{cmd.ISBN}' already exists.");

            var existingAuthorIds = await _ctx.Authors
                .Where(a => cmd.AuthorIds.Contains(a.AuthorId))
                .Select(a => a.AuthorId).ToListAsync(ct);

            var missingAuthors = cmd.AuthorIds.Except(existingAuthorIds).ToList();
            if (missingAuthors.Any())
                throw new NotFoundException(
                    $"Author IDs not found: {string.Join(", ", missingAuthors)}.");

            var existingCategoryIds = await _ctx.Categories
                .Where(c => cmd.CategoryIds.Contains(c.CategoryId))
                .Select(c => c.CategoryId).ToListAsync(ct);

            var missingCategories = cmd.CategoryIds.Except(existingCategoryIds).ToList();
            if (missingCategories.Any())
                throw new NotFoundException(
                    $"Category IDs not found: {string.Join(", ", missingCategories)}.");

            // transaction — delegate entity construction to the repository
            await _uow.BeginTransactionAsync(ct);
            try
            {
                var result = await _uow.Books.AddAsync(cmd, ct);  // ← clean call

                await _uow.CommitTransactionAsync(ct);

                _logger.LogInformation(
                    "Book created: {BookId} '{Title}' ISBN={ISBN}.",
                    result.BookId, result.Title, result.ISBN);

                return result;
            }
            catch (Exception ex) when (ex is not ConflictException
                                       && ex is not NotFoundException)
            {
                await _uow.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Failed to create book with ISBN '{ISBN}'.", cmd.ISBN);
                throw;
            }
        }

        public async Task DeleteAsync(int bookId, CancellationToken ct = default)
        {
            // 1. Confirm the book exists and load with relationships
            var book = await _ctx.Books
                .Include(b => b.Authors)
                .Include(b => b.Categories)
                .FirstOrDefaultAsync(b => b.BookId == bookId, ct)
                ?? throw new NotFoundException("Book", bookId);

            // 2. Check for active loans
            var hasActiveLoans = await _ctx.BookCopies
                .AsNoTracking()
                .AnyAsync(c => c.BookId == bookId && c.IsAvailable == false, ct);

            // 3. If active loans exist, throw a conflict exception
            if (hasActiveLoans)
                throw new ConflictException(
                    $"Cannot delete book ID {bookId} because it has active loans.");

            // 4. If no active loans, proceed with deletion
            await _uow.BeginTransactionAsync(ct);
            try
            {
                // Clear many-to-many relationships
                book.Authors.Clear();
                book.Categories.Clear();

                await _uow.Books.DeleteCopiesByBookIdAsync(bookId, ct);
                await _uow.Books.DeleteAsync(bookId, ct);
                await _uow.CommitTransactionAsync(ct);
                _logger.LogInformation("Book {BookId} deleted.", bookId);

            }
            catch (Exception ex) when (ex is not NotFoundException && ex is not ConflictException)
            {
                await _uow.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Failed to delete book {BookId}.", bookId);
                throw;
            }
        }

        public async Task<BookDto> UpdateAsync(
            int bookId, UpdateBookCommand cmd, CancellationToken ct = default)
        {
            // ── 1. Confirm the book exists ────────────────────────────────────
            var existing = await _uow.Books.GetTrackedAsync(bookId, ct)
                ?? throw new NotFoundException("Book", bookId);

            // ── 2. ISBN conflict check (only if ISBN is provided) ────────────
            if (cmd.ISBN != null)
            {
                var isbnOwner = await _ctx.Books
                    .AsNoTracking()
                    .Where(b => b.Isbn == cmd.ISBN && b.BookId != bookId)
                    .Select(b => b.BookId)
                    .FirstOrDefaultAsync(ct);

                if (isbnOwner != default)
                    throw new ConflictException(
                        $"ISBN '{cmd.ISBN}' is already used by book ID {isbnOwner}.");
            }

            // ── 3. Validate author IDs exist (only if AuthorIds is provided) ──
            if (cmd.AuthorIds != null)
            {
                var foundAuthorIds = await _ctx.Authors
                    .Where(a => cmd.AuthorIds.Contains(a.AuthorId))
                    .Select(a => a.AuthorId)
                    .ToListAsync(ct);

                var missingAuthors = cmd.AuthorIds.Except(foundAuthorIds).ToList();
                if (missingAuthors.Any())
                    throw new NotFoundException(
                        $"Author IDs not found: {string.Join(", ", missingAuthors)}.");
            }

            // ── 4. Validate category IDs exist (only if CategoryIds is provided) ─
            if (cmd.CategoryIds != null)
            {
                var foundCategoryIds = await _ctx.Categories
                    .Where(c => cmd.CategoryIds.Contains(c.CategoryId))
                    .Select(c => c.CategoryId)
                    .ToListAsync(ct);

                var missingCategories = cmd.CategoryIds.Except(foundCategoryIds).ToList();
                if (missingCategories.Any())
                    throw new NotFoundException(
                        $"Category IDs not found: {string.Join(", ", missingCategories)}.");
            }

            // ── 5. Transaction: Raw SQL execution ─────────────────────────────
            // Now uses native SQL instead of EF change tracking
            await _uow.BeginTransactionAsync(ct);
            try
            {
                await _uow.Books.UpdateAsync(bookId, cmd, ct);
                // ↑ This now executes raw SQL instead of EF's SaveChanges logic

                await _uow.CommitTransactionAsync(ct);

                var authorIdsStr = cmd.AuthorIds != null ? string.Join(", ", cmd.AuthorIds) : "(unchanged)";
                var categoryIdsStr = cmd.CategoryIds != null ? string.Join(", ", cmd.CategoryIds) : "(unchanged)";
                _logger.LogInformation(
                    "Book {BookId} '{Title}' updated via raw SQL. Authors: [{Authors}]. Categories: [{Categories}].",
                    bookId,
                    cmd.Title ?? "(unchanged)",
                    authorIdsStr,
                    categoryIdsStr);
            }
            catch (Exception ex) when (ex is not NotFoundException
                                       && ex is not ConflictException)
            {
                await _uow.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Failed to update book {BookId}.", bookId);
                throw;
            }

            // ── 6. Return fresh BookDto ──────────────────────────────────────
            return await _uow.Books.GetByIdAsync(bookId, ct)
                ?? throw new NotFoundException("Book", bookId);
        }
    }

}
