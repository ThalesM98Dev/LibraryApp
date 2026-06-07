using LibraryApp.Core.DTOs;
using LibraryApp.Core.Exceptions;
using LibraryApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApp.Data.Services;

public class AuthorService : IAuthorService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorRepository _authorRepo;
    private readonly ILogger<AuthorService> _logger;
    private readonly LibraryContext _ctx;

    public AuthorService(
        IUnitOfWork uow,
        IAuthorRepository authorRepo,
        ILogger<AuthorService> logger,
        LibraryContext ctx
        )
    {
        _uow = uow;
        _authorRepo = authorRepo;
        _logger = logger;
        _ctx = ctx;
    }

    public Task<IEnumerable<AuthorDto>> GetAllAsync(CancellationToken ct = default)
        => _authorRepo.GetAllAsync(ct);

    public Task<AuthorDto?> GetByIdAsync(int authorId, CancellationToken ct = default)
        => _authorRepo.GetByIdAsync(authorId, ct);

    public async Task<CreateAuthorResult> CreateAsync(
        CreateAuthorCommand command,
        CancellationToken ct = default)
    {
        // ── 1. Duplicate name check (business rule) ────────────────────
        if (await _authorRepo.FullNameExistsAsync(command.FullName, ct))
            throw new ConflictException(
                $"An author named '{command.FullName}' already exists.");

        // ── 2. Transaction: INSERT author ──────────────────────────────
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var result = await _authorRepo.CreateAsync(command, ct);
            // ↑ Uses raw SQL INSERT

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Author created: AuthorId={AuthorId}, FullName={FullName}",
                result.AuthorId, result.FullName);

            return result;
        }
        catch (Exception ex) when (ex is not ConflictException)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex,
                "Failed to create author: {FullName}", command.FullName);
            throw;
        }
    }

    public async Task<AuthorDto> UpdateAsync(int authorId, UpdateAuthorCommand cmd, CancellationToken ct = default)
    {
        var existingAuthor = await _uow.Authors.GetTrackedAsync(authorId, ct) ?? throw new NotFoundException($"Author with ID {authorId} not found.");

        var authorOwnerId = await _ctx.Authors
            .AsNoTracking()
            .Where(a => a.FullName == cmd.FullName && a.AuthorId != authorId)
            .Select(a => a.AuthorId)
            .FirstOrDefaultAsync(ct);

        if (authorOwnerId != default)
            throw new ConflictException($"FullName '{cmd.FullName}' is already used by Author ID {authorOwnerId}.");

        // Prepare the new full name
        var newFullName = string.IsNullOrWhiteSpace(cmd.FullName) ? existingAuthor.CurrentFullName : cmd.FullName;

        await _uow.BeginTransactionAsync(ct);
        try
        {
            await _uow.Authors.UpdateAsync(authorId, cmd, ct);
            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Author updated: AuthorId={AuthorId}, FullName={FullName}",
                authorId, newFullName);
        }
        catch (Exception ex) when (ex is not ConflictException)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex,
                "Failed to update author: AuthorId={AuthorId}, FullName={FullName}",
                authorId, newFullName);
            throw;
        }

        // Retrieve the updated author details
        var updatedAuthor = await _authorRepo.GetByIdAsync(authorId, ct)
            ?? throw new NotFoundException($"Author with ID {authorId} not found after update.");

        return updatedAuthor;
    }
}