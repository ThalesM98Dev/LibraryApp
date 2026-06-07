using LibraryApp.Core.DTOs;

namespace LibraryApp.Core.Interfaces;

public interface IMemberService
{
    Task<IEnumerable<MemberDto>> GetAllAsync(CancellationToken ct = default);
    Task<MemberDto?> GetByIdAsync(int memberId, CancellationToken ct = default);
    Task<CreateMemberResult> CreateAsync(CreateMemberCommand command, CancellationToken ct = default);
    Task<MemberDto> UpdateAsync(int memberId, UpdateMemberCommand command, CancellationToken ct = default);
    Task DeleteAsync(int memberId, CancellationToken ct = default);
}
