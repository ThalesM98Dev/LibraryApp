using System.Globalization;
using System.Text;
using LibraryApp.Core.DTOs;
using LibraryApp.Core.Exceptions;
using LibraryApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApp.Data.Services;

public class LoanService : ILoanService
{
    private readonly IUnitOfWork _uow;
    private readonly LibraryContext _ctx;
    private readonly ILogger<LoanService> _logger;

    public LoanService(IUnitOfWork uow, LibraryContext ctx, ILogger<LoanService> logger)
    {
        _uow = uow;
        _ctx = ctx;
        _logger = logger;
    }

    public Task<IEnumerable<ActiveLoanDto>> GetActiveLoansAsync(CancellationToken ct = default)
        => _uow.Loans.GetActiveLoansAsync(ct);

    public async Task<byte[]> ExportLoansToCsvAsync(CancellationToken ct = default)
    {
        var loans = await _uow.Loans.GetLoansForExportAsync(ct);

        var csv = new StringBuilder();
        csv.AppendLine("LoanId,MemberName,BookTitle,ISBN,AuthorNames,LoanDate,DueDate,ReturnDate,Fine,Status");

        foreach (var l in loans)
        {
            csv.AppendLine(string.Join(",",
                l.LoanId,
                CsvEscape(l.MemberName),
                CsvEscape(l.BookTitle),
                CsvEscape(l.Isbn),
                CsvEscape(l.AuthorNames),
                l.LoanDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                l.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                l.ReturnDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                l.Fine.ToString("F2", CultureInfo.InvariantCulture),
                l.Status
            ));
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    public async Task<LoanDto> CheckOutAsync(CheckOutCommand cmd, CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var availableCopy = await _uow.Books.GetAvailableCopyAsync(cmd.BookId, ct)
                ?? throw new ConflictException("No available copies for this book.");

            // Fetch the tracked entity separately to update it
            var trackedCopy = await _ctx.BookCopies.FindAsync(new object[] { availableCopy.CopyId }, ct);
            trackedCopy!.IsAvailable = false;
            var member = await _ctx.Members.FindAsync(new object[] { cmd.MemberId }, ct)
                ?? throw new NotFoundException("Member", cmd.MemberId);

            if (!member.IsActive)
                throw new ValidationException(new()
                {
                    ["member"] = ["Member account is not active."]
                });

            bool hasFines = await _ctx.Loans
                .AnyAsync(l => l.MemberId == cmd.MemberId && l.Fine > 0 && l.ReturnDate != null, ct);
            if (hasFines)
                throw new ConflictException("Member has outstanding unpaid fines.");

            _uow.Loans.Add(cmd.MemberId, availableCopy.CopyId);

            var copy = await _ctx.BookCopies.FindAsync(new object[] { availableCopy.CopyId }, ct);
            copy!.IsAvailable = false;

            await _uow.CommitTransactionAsync(ct);

            var loan = await _ctx.Loans
                .Where(l => l.MemberId == cmd.MemberId && l.CopyId == availableCopy.CopyId && l.ReturnDate == null)
                .OrderByDescending(l => l.LoanId)
                .Select(l => new LoanDto(l.LoanId, l.MemberId, l.CopyId, l.LoanDate, l.DueDate))
                .FirstAsync(ct);

            _logger.LogInformation(
                "Loan {LoanId} created: member {MemberId}, copy {CopyId}",
                loan.LoanId, loan.MemberId, loan.CopyId);

            return loan;
        }
        catch (Exception ex) when (ex is not NotFoundException
                                   && ex is not ConflictException
                                   && ex is not ValidationException)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex,
                "Checkout failed for member {MemberId}, book {BookId}",
                cmd.MemberId, cmd.BookId);
            throw;
        }
    }

    public async Task ReturnAsync(int loanId, CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var loan = await _ctx.Loans
                .Include(l => l.Copy)
                .FirstOrDefaultAsync(l => l.LoanId == loanId, ct)
                ?? throw new NotFoundException("Loan", loanId);

            if (loan.ReturnDate != null)
                throw new ConflictException("This loan has already been returned.");

            loan.ReturnDate = DateTime.UtcNow;
            loan.Fine = loan.CalculateFine();
            loan.Copy.IsAvailable = true;

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Loan {LoanId} returned. Fine charged: {Fine:C}", loanId, loan.Fine);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
    
}
