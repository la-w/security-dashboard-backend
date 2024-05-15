using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SecurityDashboard_Backend;

public class SecDashboardContext : DbContext
{
    private readonly string? _databasePath;

    public SecDashboardContext()
    {
        // Use a database file in the same directory as the application
        var folder = AppContext.BaseDirectory;
        _databasePath = Path.Combine(folder, "SecurityDashboard.sqlite");
    }

    public SecDashboardContext(string databasePath)
    {
        _databasePath = databasePath;
    }

    public SecDashboardContext(DbContextOptions<SecDashboardContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = _databasePath };
        optionsBuilder.UseSqlite(connectionStringBuilder.ToString());
    }
}
