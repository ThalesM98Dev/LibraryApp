using LibraryApp.Core.Exceptions;
using LibraryApp.Core.Interfaces;
using LibraryApp.Data;
using LibraryApp.Data.Generated;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace LibraryApp.Data.Repositories;

public class BookRepository : IBookRepository
{
    private readonly LibraryContext _ctx;
    private readonly ILogger<BookRepository> _logger;

    public BookRepository(LibraryContext ctx, ILogger<BookRepository> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task<PagedResult<BookDto>> SearchAsync(
        BookSearchQuery query, CancellationToken ct = default)
    {
        var q = _ctx.Books
            .Include(b => b.BookCopies)
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Title))
            q = q.Where(b => b.Title.Contains(query.Title));

        if (!string.IsNullOrWhiteSpace(query.AuthorName))
            q = q.Where(b => b.Authors
                .Any(a => a.FullName.Contains(query.AuthorName)));

        if (query.CategoryId.HasValue)
            q = q.Where(b => b.Categories
                .Any(c => c.CategoryId == query.CategoryId));

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(b => b.Title)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BookDto(
                b.BookId,
                b.Isbn,
                b.Title,
                b.PublicationYear,
                b.BookCopies.Count(c => c.IsAvailable),
                b.Authors.Select(a => a.FullName),
                b.Categories.Select(c => c.Name)
            ))
            .ToListAsync(ct);

        return new PagedResult<BookDto>(items, total, query.Page, query.PageSize);
    }

    // Lock copy row for checkout (prevents race conditions)
    public async Task<int?> GetAvailableCopyIdAsync(int bookId, CancellationToken ct = default)
    {
        var copy = await _ctx.BookCopies
            .FromSqlRaw(
                "SELECT * FROM BookCopies WITH (UPDLOCK) WHERE BookId = {0} AND IsAvailable = 1",
                bookId)
            .FirstOrDefaultAsync(ct);

        return copy?.CopyId;
    }

    public async Task<BookDto?> GetByIdAsync(int bookId, CancellationToken ct = default)
        => await _ctx.Books
            .Include(b => b.BookCopies)
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .Where(b => b.BookId == bookId)
            .Select(b => new BookDto(
                b.BookId, b.Isbn, b.Title, b.PublicationYear,
                b.BookCopies.Count(c => c.IsAvailable),
                b.Authors.Select(a => a.FullName),
                b.Categories.Select(c => c.Name)
            ))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

    public async Task<AvailableCopyDto?> GetAvailableCopyAsync(int bookId, CancellationToken ct = default)
    {
        var copy = await _ctx.BookCopies
            .FromSqlRaw("SELECT * FROM BookCopies WITH (UPDLOCK) WHERE BookId = {0} AND IsAvailable = 1", bookId)
            .FirstOrDefaultAsync();

        return copy is null ? null : new AvailableCopyDto(copy.CopyId, copy.BookId);
    }

    public async Task<bool> IsbnExistsAsync(
        string isbn, CancellationToken ct = default)
        => await _ctx.Books.AsNoTracking().AnyAsync(b => b.Isbn == isbn, ct);


    public async Task<CreateBookResult> AddAsync(
        CreateBookCommand cmd, CancellationToken ct = default)
    {
        var book = new Book                        // Book is only used in Data
        {
            Isbn = cmd.ISBN,
            Title = cmd.Title,
            PublicationYear = (short)cmd.PublicationYear,
            Description = cmd.Description,

            Authors = await _ctx.Authors
                .Where(a => cmd.AuthorIds.Contains(a.AuthorId))
                .ToListAsync(ct),

            Categories = await _ctx.Categories
                .Where(c => cmd.CategoryIds.Contains(c.CategoryId))
                .ToListAsync(ct),

            BookCopies = Enumerable.Range(0, cmd.InitialCopies)
                .Select(_ => new BookCopy
                {
                    Condition = cmd.CopyCondition,
                    IsAvailable = true
                })
                .ToList()
        };

        _ctx.Books.Add(book);
        // NOTE: SaveChanges is NOT called here.
        // UnitOfWork.CommitTransactionAsync() calls SaveChanges,
        // keeping the transaction boundary in the service layer.
        await _ctx.SaveChangesAsync(ct);           // EF assigns BookId here

        return new CreateBookResult(
            book.BookId,
            book.Isbn,
            book.Title,
            cmd.InitialCopies);
    }

    public async Task<TrackedBookDto?> GetTrackedAsync(
    int bookId, CancellationToken ct = default)
    {
        // AsNoTracking intentionally omitted — the service needs EF tracking
        // for change detection in UpdateAsync.
        var book = await _ctx.Books
            .FirstOrDefaultAsync(b => b.BookId == bookId, ct);

        return book is null
            ? null
            : new TrackedBookDto(book.BookId, book.Isbn, book.Title);
    }

    //public async Task UpdateAsync(int bookId, UpdateBookCommand cmd, CancellationToken ct)
    //{
    //    var book = await _ctx.Books
    //        .Include(b => b.Authors)
    //        .Include(b => b.Categories)
    //        .FirstOrDefaultAsync(b => b.BookId == bookId, ct)
    //        ?? throw new NotFoundException("Book", bookId);

    //    // Scalar fields – only update if provided
    //    if (cmd.ISBN != null) book.Isbn = cmd.ISBN;
    //    if (cmd.Title != null) book.Title = cmd.Title;
    //    if (cmd.PublicationYear.HasValue) book.PublicationYear = (short)cmd.PublicationYear.Value;
    //    if (cmd.Description != null) book.Description = cmd.Description;
    //    book.UpdatedAt = DateTime.UtcNow;

    //    // Many-to-many – only sync if provided
    //    if (cmd.AuthorIds != null)
    //    {
    //        book.Authors.Clear();
    //        var authors = await _ctx.Authors.Where(a => cmd.AuthorIds.Contains(a.AuthorId)).ToListAsync(ct);
    //        foreach (var a in authors) book.Authors.Add(a);
    //    }

    //    if (cmd.CategoryIds != null)
    //    {
    //        book.Categories.Clear();
    //        var categories = await _ctx.Categories.Where(c => cmd.CategoryIds.Contains(c.CategoryId)).ToListAsync(ct);
    //        foreach (var c in categories) book.Categories.Add(c);
    //    }

    //    // No SaveChanges – let the UnitOfWork handle it
    //}

    /// <summary>
    /// Updates a book using raw SQL with explicit transaction control.
    /// This method executes three DELETE/INSERT operations atomically:
    /// 1. UPDATE Books (scalar fields)
    /// 2. DELETE + INSERT BookAuthors (junction table sync)
    /// 3. DELETE + INSERT BookCategories (junction table sync)
    /// 
    /// NOTE: This method DOES NOT call SaveChanges().
    /// The UnitOfWork owns the transaction boundary and calls SaveChanges after this returns.
    /// </summary>
    public async Task UpdateAsync(
    int bookId,
    UpdateBookCommand cmd,
    CancellationToken ct = default)
    {
        // Get the pooled connection — already open from BeginTransactionAsync()
        var connection = _ctx.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        // ✓ CRITICAL: Get the DbTransaction from the DbContext
        // This is the transaction that BeginTransactionAsync() started
        var dbTransaction = _ctx.Database.CurrentTransaction?.GetDbTransaction();

        if (dbTransaction == null)
            throw new InvalidOperationException("No active transaction. Call BeginTransactionAsync() first.");

        try
        {
            // ─────────────────────────────────────────────────────────
            // STEP 1: UPDATE Books table (scalar fields) - only update provided fields
            // ─────────────────────────────────────────────────────────

            // Build dynamic SET clause based on provided fields
            var setClauses = new List<string>();
            if (cmd.ISBN != null) setClauses.Add("Isbn = @Isbn");
            if (cmd.Title != null) setClauses.Add("Title = @Title");
            if (cmd.PublicationYear.HasValue) setClauses.Add("PublicationYear = @Year");
            if (cmd.Description != null) setClauses.Add("Description = @Description");
            setClauses.Add("UpdatedAt = SYSUTCDATETIME()");

            string updateBooksQuery = $@"
            UPDATE Books
            SET {string.Join(", ", setClauses)}
            WHERE BookId = @BookId";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = updateBooksQuery;
                command.CommandTimeout = 30;
                // ✓ REQUIRED: Assign the transaction to the command
                command.Transaction = dbTransaction;

                // Add parameters
                AddParameter(command, "@BookId", DbType.Int32, bookId);
                if (cmd.ISBN != null) AddParameter(command, "@Isbn", DbType.String, cmd.ISBN);
                if (cmd.Title != null) AddParameter(command, "@Title", DbType.String, cmd.Title);
                if (cmd.PublicationYear.HasValue) AddParameter(command, "@Year", DbType.Int16, cmd.PublicationYear.Value);
                if (cmd.Description != null) AddParameter(command, "@Description", DbType.String, cmd.Description);

                int rowsAffected = await command.ExecuteNonQueryAsync(ct);

                if (rowsAffected == 0)
                    throw new NotFoundException("Book", bookId);

                _logger.LogDebug(
                    "Updated Books row: BookId={BookId}, Title={Title}, UpdatedAt=SYSUTCDATETIME()",
                    bookId, cmd.Title ?? "(unchanged)");
            }

            // ─────────────────────────────────────────────────────────
            // STEP 2: Sync BookAuthors (full replace pattern) - only if provided
            // ─────────────────────────────────────────────────────────

            if (cmd.AuthorIds != null)
            {
                const string deleteAuthorsQuery = @"
                DELETE FROM BookAuthors
                WHERE BookId = @BookId";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = deleteAuthorsQuery;
                    command.CommandTimeout = 30;
                    // ✓ REQUIRED: Assign the transaction
                    command.Transaction = dbTransaction;

                    AddParameter(command, "@BookId", DbType.Int32, bookId);

                    int deleted = await command.ExecuteNonQueryAsync(ct);
                    _logger.LogDebug("Deleted {Count} old BookAuthor links for BookId={BookId}",
                        deleted, bookId);
                }

                // Insert the new author links
                if (cmd.AuthorIds.Count > 0)
                {
                    string insertAuthorsQuery = BuildMultipleInsertQuery(
                        "BookAuthors",
                        new[] { "BookId", "AuthorId" },
                        cmd.AuthorIds.Count);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = insertAuthorsQuery;
                        command.CommandTimeout = 30;
                        // ✓ REQUIRED: Assign the transaction
                        command.Transaction = dbTransaction;

                        AddParameter(command, "@BookId", DbType.Int32, bookId);
                        for (int i = 0; i < cmd.AuthorIds.Count; i++)
                            AddParameter(command, $"@AuthorId_{i}", DbType.Int32, cmd.AuthorIds[i]);

                        int inserted = await command.ExecuteNonQueryAsync(ct);
                        _logger.LogDebug(
                            "Inserted {Count} new BookAuthor links for BookId={BookId}",
                            inserted, bookId);
                    }
                }
            }

            // ─────────────────────────────────────────────────────────
            // STEP 3: Sync BookCategories (full replace pattern) - only if provided
            // ─────────────────────────────────────────────────────────

            if (cmd.CategoryIds != null)
            {
                const string deleteCategoriesQuery = @"
                DELETE FROM BookCategories
                WHERE BookId = @BookId";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = deleteCategoriesQuery;
                    command.CommandTimeout = 30;
                    // ✓ REQUIRED: Assign the transaction
                    command.Transaction = dbTransaction;

                    AddParameter(command, "@BookId", DbType.Int32, bookId);

                    int deleted = await command.ExecuteNonQueryAsync(ct);
                    _logger.LogDebug(
                        "Deleted {Count} old BookCategory links for BookId={BookId}",
                        deleted, bookId);
                }

                // Insert the new category links
                if (cmd.CategoryIds.Count > 0)
                {
                    string insertCategoriesQuery = BuildMultipleInsertQuery(
                        "BookCategories",
                        new[] { "BookId", "CategoryId" },
                        cmd.CategoryIds.Count);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = insertCategoriesQuery;
                        command.CommandTimeout = 30;
                        // ✓ REQUIRED: Assign the transaction
                        command.Transaction = dbTransaction;

                        AddParameter(command, "@BookId", DbType.Int32, bookId);
                        for (int i = 0; i < cmd.CategoryIds.Count; i++)
                            AddParameter(command, $"@CategoryId_{i}", DbType.Int32, cmd.CategoryIds[i]);

                        int inserted = await command.ExecuteNonQueryAsync(ct);
                        _logger.LogDebug(
                            "Inserted {Count} new BookCategory links for BookId={BookId}",
                            inserted, bookId);
                    }
                }
            }

            var authorCount = cmd.AuthorIds?.Count ?? 0;
            var categoryCount = cmd.CategoryIds?.Count ?? 0;
            _logger.LogInformation(
                "Book {BookId} updated via raw SQL: {Authors} authors, {Categories} categories",
                bookId, authorCount, categoryCount);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex,
                "SQL error while updating book {BookId}: {Message}",
                bookId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while updating book {BookId}",
                bookId);
            throw;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // HELPER METHODS
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Safely adds a parameter to a SQL command, handling null values.
    /// </summary>
    private static void AddParameter(
        DbCommand command,
        string name,
        DbType type,
        object? value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.DbType = type;
        // Use ternary so both branches have a common type (object) and avoid the ?? type mismatch
        param.Value = value != null ? (object)value : DBNull.Value;
        command.Parameters.Add(param);
    }

    /// <summary>
    /// Generates a multi-row INSERT statement for bulk inserts.
    /// Example: "INSERT INTO BookAuthors (BookId, AuthorId) VALUES (@BookId, @AuthorId_0), (@BookId, @AuthorId_1)"
    /// </summary>
    private static string BuildMultipleInsertQuery(
        string tableName,
        string[] columns,
        int rowCount)
    {
        if (rowCount == 0)
            return string.Empty;

        var columnList = string.Join(", ", columns);
        var valuesList = new List<string>();

        for (int i = 0; i < rowCount; i++)
        {
            var paramList = string.Join(", ",
                columns.Select((col, colIdx) =>
                    col == "BookId" ? "@BookId" : $"@{col}_{i}"));
            valuesList.Add($"({paramList})");
        }

        return $"INSERT INTO {tableName} ({columnList}) VALUES {string.Join(", ", valuesList)}";
    }

    public async Task DeleteAsync(int bookId, CancellationToken ct = default)
    {
        var book = await _ctx.Books
            .FirstOrDefaultAsync(b => b.BookId == bookId, ct)
            ?? throw new NotFoundException("Book", bookId);

        _ctx.Books.Remove(book);
    }

    public async Task DeleteCopiesByBookIdAsync(int bookId, CancellationToken ct = default)
    {
        var copies = await _ctx.BookCopies
            .Where(c => c.BookId == bookId)
            .ToListAsync(ct);

        _ctx.BookCopies.RemoveRange(copies);
    }
}
