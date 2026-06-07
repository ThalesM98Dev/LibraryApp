using LibraryApp.Core.DTOs;

namespace LibraryApp.Core.Interfaces;

public interface ILoanRepository
{
    Task<IEnumerable<ActiveLoanDto>> GetActiveLoansAsync(CancellationToken ct = default);
    Task<LoanDto?> GetByIdAsync(int loanId, CancellationToken ct = default);
    void Add(int memberId, int copyId);
    Task<IEnumerable<LoanExportDto>> GetLoansForExportAsync(CancellationToken ct = default);
}
