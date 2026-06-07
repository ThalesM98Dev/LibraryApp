public record BookDto(
    int BookId,
    string ISBN,
    string Title,
    int PublicationYear,
    int AvailableCopies,
    IEnumerable<string> Authors,
    IEnumerable<string> Categories
);

public record BookSearchQuery(
    string? Title = null,
    string? AuthorName = null,
    int? CategoryId = null,
    int Page = 1,
    int PageSize = 20
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

// ── Request ────────────────────────────────────────────────────────────
public record CreateBookCommand(
    string ISBN,
    string Title,
    int PublicationYear,
    string? Description,
    List<int> AuthorIds,       // existing authors to link
    List<int> CategoryIds,     // existing categories to link
    int InitialCopies,   // how many physical copies to create
    string CopyCondition    // "New" | "Good" | "Fair" | "Poor"
);

// ── Response ───────────────────────────────────────────────────────────
public record CreateBookResult(
    int BookId,
    string ISBN,
    string Title,
    int CopiesCreated
);
public record AvailableCopyDto(int CopyId, int BookId);


public record UpdateBookCommand(
    string? ISBN,
    string? Title,
    int? PublicationYear,
    string? Description,
    List<int>? AuthorIds,
    List<int>? CategoryIds
);
// never serialized to the client
public record TrackedBookDto(
    int BookId,
    string CurrentIsbn,
    string Title
);

public record DeleteBookCommand(int BookId);
