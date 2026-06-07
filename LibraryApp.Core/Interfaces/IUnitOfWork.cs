using LibraryApp.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ILoanRepository Loans { get; }
    IBookRepository Books { get; }
    IAuthorRepository Authors { get; }
    IMemberRepository Members { get; }

    Task<int> CommitAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}