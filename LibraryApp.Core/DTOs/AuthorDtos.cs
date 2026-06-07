namespace LibraryApp.Core.DTOs;

// ── Request ────────────────────────────────────────────────────────
public record CreateAuthorCommand(
    string FullName,
    string? Biography
);
public record UpdateAuthorCommand(
    string? FullName,
    string? Biography
);

// ── Response ───────────────────────────────────────────────────────
public record AuthorDto(
    int AuthorId,
    string FullName,
    string? Biography
);

public record CreateAuthorResult(
    int AuthorId,
    string FullName
);
public record UpdateAuthorResult(
    int AuthorId,
    string FullName
);
public record TrackedAuthorDto(
    int AuthorId,
    string CurrentFullName
    );