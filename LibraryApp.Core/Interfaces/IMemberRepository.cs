using LibraryApp.Core.DTOs;

namespace LibraryApp.Core.Interfaces;

public interface IMemberRepository
{
    Task<IEnumerable<MemberDto>> GetAllAsync(CancellationToken ct = default);
    Task<MemberDto?> GetByIdAsync(int memberId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<CreateMemberResult> CreateAsync(CreateMemberCommand command, CancellationToken ct = default);
    Task<TrackedMemberDto?> GetTrackedAsync(int memberId, CancellationToken ct = default);
    Task UpdateAsync(int memberId, UpdateMemberCommand command, CancellationToken ct = default);
}
