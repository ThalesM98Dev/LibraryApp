using LibraryApp.Core.DTOs;
using LibraryApp.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace LibraryApp.Data.Repositories;

public class AuthorRepository : IAuthorRepository
{
    private readonly LibraryContext _ctx;
    private readonly ILogger<AuthorRepository> _logger;
    private readonly string _connectionString;


    public AuthorRepository(LibraryContext ctx, ILogger<AuthorRepository> logger)
    {
        _connectionString = ctx.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string not found.");
        _ctx = ctx;
        _logger = logger;
    }

    public async Task<IEnumerable<AuthorDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _ctx.Authors
     .AsNoTracking()
     .OrderBy(a => a.FullName)  // ✅ Order by entity property before projection
     .Select(a => new AuthorDto(a.AuthorId, a.FullName, a.Biography))
     .ToListAsync(ct);
    }

    public async Task<AuthorDto?> GetByIdAsync(int authorId, CancellationToken ct = default)
    {
        return await _ctx.Authors
            .AsNoTracking()
            .Where(a => a.AuthorId == authorId)
            .Select(a => new AuthorDto(a.AuthorId, a.FullName, a.Biography))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> FullNameExistsAsync(string fullName, CancellationToken ct = default)
    {
        return await _ctx.Authors
            .AsNoTracking()
            .AnyAsync(a => a.FullName == fullName, ct);
    }

    /// <summary>
    /// Creates a new author using raw SQL INSERT with manual transaction handling.
    /// Returns the generated AuthorId via SCOPE_IDENTITY().
    /// 
    /// NOTE: This method executes within an active transaction started by UnitOfWork.
    /// The transaction must already be open and the command must have .Transaction assigned.
    /// </summary>
    public async Task<CreateAuthorResult> CreateAsync(
        CreateAuthorCommand command,
        CancellationToken ct = default)
    {
        var connection = _ctx.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        // ✓ CRITICAL: Get the active transaction from DbContext
        var dbTransaction = _ctx.Database.CurrentTransaction?.GetDbTransaction();
        if (dbTransaction == null)
            throw new InvalidOperationException("No active transaction. Call BeginTransactionAsync() first.");

        try
        {
            // Raw SQL INSERT with SCOPE_IDENTITY() to get the generated AuthorId
            const string insertQuery = @"
                INSERT INTO Authors (FullName, Biography, CreatedAt)
                VALUES (@FullName, @Biography, SYSUTCDATETIME());
                
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using (var command_obj = connection.CreateCommand())
            {
                command_obj.CommandText = insertQuery;
                command_obj.CommandTimeout = 30;
                // ✓ REQUIRED: Assign the transaction to the command
                command_obj.Transaction = dbTransaction;

                // Add parameters — safe from SQL injection
                AddParameter(command_obj, "@FullName", DbType.String, command.FullName);
                AddParameter(command_obj, "@Biography", DbType.String, command.Biography);

                // ExecuteScalarAsync returns the SCOPE_IDENTITY() value (the new AuthorId)
                var result = await command_obj.ExecuteScalarAsync(ct);

                if (result == null || result == DBNull.Value)
                    throw new InvalidOperationException("Failed to retrieve generated AuthorId.");

                int authorId = Convert.ToInt32(result);

                _logger.LogDebug(
                    "Created author via raw SQL: AuthorId={AuthorId}, FullName={FullName}",
                    authorId, command.FullName);

                return new CreateAuthorResult(authorId, command.FullName);
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex,
                "SQL error while creating author: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating author");
            throw;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // HELPER METHOD
    // ──────────────────────────────────────────────────────────────────

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

    public async Task UpdateAsync(int authorId, UpdateAuthorCommand cmd, CancellationToken ct = default)
    {
        SqlConnection connection = null;
        SqlTransaction transaction = null;

        try
        {
            // 1. Create and open connection
            connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            // 2. Begin transaction
            transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);

            // 3. Prepare and execute the command
            const string updateSql = @"
            UPDATE Authors
            SET FullName = @FullName, Biography = @Biography
            WHERE AuthorId = @AuthorId";

            using (var command = new SqlCommand(updateSql, connection, transaction))
            {
                command.CommandTimeout = 30;
                command.Parameters.AddWithValue("@AuthorId", authorId);
                command.Parameters.AddWithValue("@FullName", cmd.FullName);
                command.Parameters.AddWithValue("@Biography", cmd.Biography ?? (object)DBNull.Value);

                int rowsAffected = await command.ExecuteNonQueryAsync(ct);

                if (rowsAffected == 0)
                {
                    throw new KeyNotFoundException($"Author with ID {authorId} not found.");
                }
            }

            // 4. Commit transaction (only if all succeeded)
            await transaction.CommitAsync(ct);
            _logger.LogInformation("Updated author via pure ADO.NET: AuthorId={AuthorId}", authorId);
        }
        catch
        {
            // 5. Rollback transaction on any exception (if still alive)
            if (transaction != null)
            {
                await transaction.RollbackAsync(ct);
            }
            throw; // rethrow the original exception
        }
        finally
        {
            // 6. Clean up resources – order matters: dispose transaction first, then connection
            transaction?.Dispose();  // Disposes transaction (rollback already done on error)
            connection?.Dispose();   // Disposes connection – automatically closes it
        }
    }
    

    public async Task<TrackedAuthorDto?> GetTrackedAsync(int authorId, CancellationToken ct = default)
    {

        var author = await _ctx.Authors
            .FirstOrDefaultAsync(a => a.AuthorId == authorId, ct);

        return author is null
            ? null
            : new TrackedAuthorDto(author.AuthorId, author.FullName);
    }
}

