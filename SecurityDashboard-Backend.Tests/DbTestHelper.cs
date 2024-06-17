using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace SecurityDashboard_Backend.Tests;

public static class DbTestHelper
{
    public static async Task<(SqliteConnection, SecDashboardContext)> InitializeDatabaseAsync()
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder();

        connectionStringBuilder.DataSource = ":memory:";
        connectionStringBuilder.Cache = SqliteCacheMode.Private;

        var connection = new SqliteConnection(connectionStringBuilder.ToString());
        await connection.OpenAsync();

        var contextOptions = new DbContextOptionsBuilder<SecDashboardContext>().UseSqlite(connection);

        var context = new SecDashboardContext(contextOptions.Options);

        if (!(await context.Database.GetAppliedMigrationsAsync()).Any())
            await context.Database.EnsureDeletedAsync();

        await context.Database.MigrateAsync();

        return (connection, context);
    }


    public static async Task DisposeDatabaseAsync(SqliteConnection connection, SecDashboardContext context)
    {
        if (connection.State != ConnectionState.Closed)
        {
            await connection.CloseAsync();
        }

        await context.DisposeAsync();
        await connection.DisposeAsync();
    }
}
