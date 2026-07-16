using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace TrainingPlatform.Application.Common.Persistence;

/// <summary>
/// Provider-agnostic classification of persistence failures. Kept in the
/// Application layer (no Npgsql reference) by reading the ANSI SQLSTATE off the
/// base <see cref="DbException"/> rather than a provider-specific exception type.
/// </summary>
public static class PersistenceErrors
{
    // ANSI/PostgreSQL SQLSTATE for a unique_violation.
    private const string UniqueViolationSqlState = "23505";

    /// <summary>
    /// True when a save failed because a row collided with a unique constraint —
    /// e.g. a concurrent request inserted the same (UserId, TopicId) first.
    /// </summary>
    public static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is DbException dbException
           && (dbException.SqlState == UniqueViolationSqlState
               // Sqlite (integration tests) surfaces it in the message, not SqlState.
               || dbException.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase));
}
