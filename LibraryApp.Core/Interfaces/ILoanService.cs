using LibraryApp.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryApp.Core.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<ActiveLoanDto>> GetActiveLoansAsync(CancellationToken ct = default);
        Task<LoanDto> CheckOutAsync(CheckOutCommand command, CancellationToken ct = default);
        Task ReturnAsync(int loanId, CancellationToken ct = default);
        Task<byte[]> ExportLoansToCsvAsync(CancellationToken ct = default);
    }
}
