using LibraryApp.Core.DTOs;
using LibraryApp.Core.Interfaces;
using LibraryApp.Data.Generated;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Repositories;

public class LoanRepository : ILoanRepository
{
    private const int DefaultLoanDays = 14;
    private readonly LibraryContext _ctx;
    private readonly string _connectionString;
    public LoanRepository(LibraryContext ctx)
    {
        _connectionString = ctx.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string not found.");
        _ctx = ctx;
    }

    public async Task<IEnumerable<ActiveLoanDto>> GetActiveLoansAsync(CancellationToken ct = default)
    {
        return await _ctx.Loans
            .AsNoTracking()
            .Where(l => l.ReturnDate == null)
            .OrderBy(l => l.DueDate)
            .Select(l => new ActiveLoanDto(
                l.LoanId,
                l.Member.FullName,
                l.Copy.Book.Title,
                l.Copy.Book.Authors
                    .Select(a => a.FullName)
                    .FirstOrDefault() ?? "Unknown",
                l.DueDate,
                l.DueDate < DateTime.UtcNow
            ))
            .ToListAsync(ct);
    }

    public async Task<LoanDto?> GetByIdAsync(int loanId, CancellationToken ct = default)
        => await _ctx.Loans
            .Where(l => l.LoanId == loanId)
            .Select(l => new LoanDto(
                l.LoanId,
                l.MemberId,
                l.CopyId,
                l.LoanDate,
                l.DueDate))
            .FirstOrDefaultAsync(ct);

    public void Add(int memberId, int copyId)
    {
        var now = DateTime.UtcNow;
        _ctx.Loans.Add(new Loan
        {
            MemberId = memberId,
            CopyId = copyId,
            LoanDate = now,
            DueDate = now.AddDays(DefaultLoanDays),
            CreatedAt = now
        });
    }

    public async Task<IEnumerable<LoanExportDto>> GetLoansForExportAsync(CancellationToken ct = default)
    {
        const string sql = @"
    SELECT
        l.LoanId,
        m.FullName AS MemberName,
        b.Title AS BookTitle,
        b.Isbn,
        STRING_AGG(a.FullName, '; ') AS AuthorNames,
        l.LoanDate,
        l.DueDate,
        l.ReturnDate,
        l.Fine,
        CASE
            WHEN l.ReturnDate IS NOT NULL THEN 'Returned'
            WHEN l.DueDate < SYSDATETIME() THEN 'Overdue'
            ELSE 'Active'
        END AS Status
    FROM Loans l
        INNER JOIN Members m ON m.MemberId = l.MemberId
        INNER JOIN BookCopies bc ON bc.CopyId = l.CopyId
        INNER JOIN Books b ON b.BookId = bc.BookId
        LEFT JOIN BookAuthors ba ON ba.BookId = b.BookId
        LEFT JOIN Authors a ON a.AuthorId = ba.AuthorId
    GROUP BY
        l.LoanId, m.FullName, b.Title, b.Isbn,
        l.LoanDate, l.DueDate, l.ReturnDate, l.Fine
    ORDER BY l.LoanDate DESC";

        var loans = new List<LoanExportDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        await using var command = new SqlCommand(sql, connection, transaction);
        command.CommandTimeout = 120;

        try
        {
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                loans.Add(new LoanExportDto(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? "" : reader.GetString(4),
                    reader.GetDateTime(5),
                    reader.GetDateTime(6),
                    reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    reader.GetDecimal(8),
                    reader.GetString(9)
                ));

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return loans;
    }
}
