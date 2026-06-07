using LibraryApp.Core.DTOs;
using LibraryApp.Core.Exceptions;
using LibraryApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApp.Data.Services;

public class MemberService : IMemberService
{
    private readonly IUnitOfWork _uow;
    private readonly IMemberRepository _memberRepo;
    private readonly ILogger<MemberService> _logger;
    private readonly LibraryContext _ctx;

    public MemberService(
        IUnitOfWork uow,
        IMemberRepository memberRepo,
        ILogger<MemberService> logger,
        LibraryContext ctx)
    {
        _uow = uow;
        _memberRepo = memberRepo;
        _logger = logger;
        _ctx = ctx;
    }

    public Task<IEnumerable<MemberDto>> GetAllAsync(CancellationToken ct = default)
        => _memberRepo.GetAllAsync(ct);

    public Task<MemberDto?> GetByIdAsync(int memberId, CancellationToken ct = default)
        => _memberRepo.GetByIdAsync(memberId, ct);

    public async Task<CreateMemberResult> CreateAsync(
        CreateMemberCommand command,
        CancellationToken ct = default)
    {
        if (await _memberRepo.EmailExistsAsync(command.Email, ct))
            throw new ConflictException(
                $"A member with email '{command.Email}' already exists.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var result = await _memberRepo.CreateAsync(command, ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Member created: MemberId={MemberId}, FullName={FullName}, Email={Email}",
                result.MemberId, result.FullName, result.Email);

            return result;
        }
        catch (Exception ex) when (ex is not ConflictException)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex,
                "Failed to create member: {FullName}, {Email}",
                command.FullName, command.Email);
            throw;
        }
    }

    public async Task<MemberDto> UpdateAsync(int memberId, UpdateMemberCommand cmd, CancellationToken ct = default)
    {
        var existingMember = await _uow.Members.GetTrackedAsync(memberId, ct)
            ?? throw new NotFoundException($"Member with ID {memberId} not found.");

        if (!string.IsNullOrWhiteSpace(cmd.Email))
        {
            var emailOwnerId = await _ctx.Members
                .AsNoTracking()
                .Where(m => m.Email == cmd.Email && m.MemberId != memberId)
                .Select(m => m.MemberId)
                .FirstOrDefaultAsync(ct);

            if (emailOwnerId != default)
                throw new ConflictException($"Email '{cmd.Email}' is already used by Member ID {emailOwnerId}.");
        }

        var newFullName = string.IsNullOrWhiteSpace(cmd.FullName) ? existingMember.CurrentFullName : cmd.FullName;
        var newEmail = string.IsNullOrWhiteSpace(cmd.Email) ? existingMember.CurrentEmail : cmd.Email;
        var newStatus = string.IsNullOrWhiteSpace(cmd.Status) ? existingMember.CurrentStatus : cmd.Status;

        var updateCmd = new UpdateMemberCommand(newFullName, newEmail, newStatus);

        await _uow.BeginTransactionAsync(ct);
        try
        {
            await _uow.Members.UpdateAsync(memberId, updateCmd, ct);
            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Member updated: MemberId={MemberId}, FullName={FullName}, Email={Email}",
                memberId, newFullName, newEmail);
        }
        catch (Exception ex) when (ex is not ConflictException)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex,
                "Failed to update member: MemberId={MemberId}", memberId);
            throw;
        }

        var updatedMember = await _memberRepo.GetByIdAsync(memberId, ct)
            ?? throw new NotFoundException($"Member with ID {memberId} not found after update.");

        return updatedMember;
    }
}
