using LibraryApp.Core.DTOs;
using LibraryApp.Core.Interfaces;
using LibraryApp.Data.Generated;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace LibraryApp.Data.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly LibraryContext _ctx;
    private readonly ILogger<MemberRepository> _logger;
    private readonly string _connectionString;

    public MemberRepository(LibraryContext ctx, ILogger<MemberRepository> logger)
    {
        _connectionString = ctx.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string not found.");
        _ctx = ctx;
        _logger = logger;
    }

    public async Task<IEnumerable<MemberDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _ctx.Members
            .AsNoTracking()
            .OrderBy(m => m.FullName)
            .Select(m => new MemberDto(
                m.MemberId,
                m.FullName,
                m.Email,
                m.MemberSince,
                m.Status))
            .ToListAsync(ct);
    }

    public async Task<MemberDto?> GetByIdAsync(int memberId, CancellationToken ct = default)
    {
        return await _ctx.Members
            .AsNoTracking()
            .Where(m => m.MemberId == memberId)
            .Select(m => new MemberDto(
                m.MemberId,
                m.FullName,
                m.Email,
                m.MemberSince,
                m.Status))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        return await _ctx.Members
            .AsNoTracking()
            .AnyAsync(m => m.Email == email, ct);
    }

    public async Task<CreateMemberResult> CreateAsync(
        CreateMemberCommand command,
        CancellationToken ct = default)
    {
        var connection = _ctx.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        var dbTransaction = _ctx.Database.CurrentTransaction?.GetDbTransaction();
        if (dbTransaction == null)
            throw new InvalidOperationException("No active transaction. Call BeginTransactionAsync() first.");

        try
        {
            const string insertQuery = @"
                INSERT INTO Members (FullName, Email, MemberSince, Status, CreatedAt, UpdatedAt)
                VALUES (@FullName, @Email, SYSUTCDATETIME(), 'Active', SYSUTCDATETIME(), SYSUTCDATETIME());
                
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using (var command_obj = connection.CreateCommand())
            {
                command_obj.CommandText = insertQuery;
                command_obj.CommandTimeout = 30;
                command_obj.Transaction = dbTransaction;

                AddParameter(command_obj, "@FullName", DbType.String, command.FullName);
                AddParameter(command_obj, "@Email", DbType.String, command.Email);

                var result = await command_obj.ExecuteScalarAsync(ct);

                if (result == null || result == DBNull.Value)
                    throw new InvalidOperationException("Failed to retrieve generated MemberId.");

                int memberId = Convert.ToInt32(result);

                _logger.LogDebug(
                    "Created member via raw SQL: MemberId={MemberId}, FullName={FullName}, Email={Email}",
                    memberId, command.FullName, command.Email);

                return new CreateMemberResult(memberId, command.FullName, command.Email);
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex,
                "SQL error while creating member: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating member");
            throw;
        }
    }

    public async Task<TrackedMemberDto?> GetTrackedAsync(int memberId, CancellationToken ct = default)
    {
        var member = await _ctx.Members
            .FirstOrDefaultAsync(m => m.MemberId == memberId, ct);

        return member is null
            ? null
            : new TrackedMemberDto(member.MemberId, member.FullName, member.Email, member.Status);
    }

    public async Task UpdateAsync(int memberId, UpdateMemberCommand cmd, CancellationToken ct = default)
    {
        SqlConnection connection = null;
        SqlTransaction transaction = null;

        try
        {
            connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);

            const string updateSql = @"
                UPDATE Members
                SET FullName = @FullName, Email = @Email, 
                    Status = ISNULL(@Status, Status), 
                    UpdatedAt = SYSUTCDATETIME()
                WHERE MemberId = @MemberId";

            using (var command = new SqlCommand(updateSql, connection, transaction))
            {
                command.CommandTimeout = 30;
                command.Parameters.AddWithValue("@MemberId", memberId);
                command.Parameters.AddWithValue("@FullName", cmd.FullName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Email", cmd.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", cmd.Status ?? (object)DBNull.Value);

                int rowsAffected = await command.ExecuteNonQueryAsync(ct);

                if (rowsAffected == 0)
                {
                    throw new KeyNotFoundException($"Member with ID {memberId} not found.");
                }
            }

            await transaction.CommitAsync(ct);
            _logger.LogInformation("Updated member via pure ADO.NET: MemberId={MemberId}", memberId);
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(ct);
            }
            throw;
        }
        finally
        {
            transaction?.Dispose();
            connection?.Dispose();
        }
    }

    public async Task DeleteAsync(int memberId, CancellationToken ct = default)
    {
        var member = await _ctx.Members
            .FirstOrDefaultAsync(m => m.MemberId == memberId, ct);

        if (member == null)
            throw new KeyNotFoundException($"Member with ID {memberId} not found.");

        _ctx.Members.Remove(member);

        _logger.LogInformation("Deleted member: MemberId={MemberId}", memberId);
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        DbType type,
        object? value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.DbType = type;
        param.Value = value ?? DBNull.Value;
        command.Parameters.Add(param);
    }
}
