using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace SecurityDashboard_Backend.Tests;

public class DbTestBase : IAsyncLifetime
{
    protected SecDashboardContext Context = null!; // Suppress warning, will be initialized in InitializeAsync
    private SqliteConnection _connection = null!;
    protected readonly ITestOutputHelper TestOutputHelper;

    public DbTestBase(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    public virtual async Task InitializeAsync()
    {
        (_connection, Context) = await DbTestHelper.InitializeDatabaseAsync();
    }


    public virtual async Task DisposeAsync()
    {
        await DbTestHelper.DisposeDatabaseAsync(_connection, Context);
    }
}
