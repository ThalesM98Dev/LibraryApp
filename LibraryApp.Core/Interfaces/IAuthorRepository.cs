using LibraryApp.Core.DTOs;

namespace LibraryApp.Core.Interfaces;

public interface IAuthorRepository
{
    Task<IEnumerable<AuthorDto>> GetAllAsync(CancellationToken ct = default);
    Task<AuthorDto?> GetByIdAsync(int authorId, CancellationToken ct = default);
    Task<bool> FullNameExistsAsync(string fullName, CancellationToken ct = default);
    Task<CreateAuthorResult> CreateAsync(CreateAuthorCommand command, CancellationToken ct = default);
    Task<TrackedAuthorDto?> GetTrackedAsync(int authorId, CancellationToken ct = default);
    Task UpdateAsync(int authorId, UpdateAuthorCommand command, CancellationToken ct = default);
}