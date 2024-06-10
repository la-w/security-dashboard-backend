using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.DomainModel;

namespace SecurityDashboard_Backend;

public class SecDashboardContext : DbContext
{
    private readonly string? _databasePath;

    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();
    public DbSet<Finding> Findings => Set<Finding>();

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Finding>()
            .HasDiscriminator(x => x.Tool)
            .HasValue<FluidAttacksFinding>(SarifTool.FluidAttacks)
            .HasValue<GitLeaksFinding>(SarifTool.GitLeaks)
            .HasValue<OwaspDependencyCheckFinding>(SarifTool.OwaspDependencyCheck);
    }
}
