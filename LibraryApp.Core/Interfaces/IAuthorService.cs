using LibraryApp.Core.DTOs;

namespace LibraryApp.Core.Interfaces;

public interface IAuthorService
{
    Task<IEnumerable<AuthorDto>> GetAllAsync(CancellationToken ct = default);
    Task<AuthorDto?> GetByIdAsync(int authorId, CancellationToken ct = default);
    Task<CreateAuthorResult> CreateAsync(CreateAuthorCommand command, CancellationToken ct = default);

    Task<AuthorDto> UpdateAsync(
      int authorId, UpdateAuthorCommand command, CancellationToken ct = default);
}