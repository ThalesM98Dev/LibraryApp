using LibraryApp.Core.Interfaces;
using LibraryApp.Data;
using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork : IUnitOfWork
{
    private readonly LibraryContext _ctx;
    private IDbContextTransaction? _transaction;

    public ILoanRepository Loans { get; }
    public IBookRepository Books { get; }
    public IAuthorRepository Authors { get; }
    public IMemberRepository Members { get; }

    public UnitOfWork(LibraryContext ctx, ILoanRepository loans, IBookRepository books, IAuthorRepository authors, IMemberRepository members)
    {
        _ctx = ctx;
        Loans = loans;
        Books = books;
        Authors = authors;
        Members = members;
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _ctx.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction == null) throw new InvalidOperationException("No active transaction.");
        await _ctx.SaveChangesAsync(ct);
        await _transaction.CommitAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
            await _transaction.RollbackAsync(ct);
    }

    public Task<int> CommitAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);

    public void Dispose()
    {
        _transaction?.Dispose();
        _ctx.Dispose();
    }
}