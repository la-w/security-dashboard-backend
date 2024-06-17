using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.DomainModel;

namespace SecurityDashboard_Backend;

public class SecDashboardContext : DbContext
{
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<ToolRule> ToolRules => Set<ToolRule>();

    public SecDashboardContext(DbContextOptions<SecDashboardContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Finding>()
            .HasDiscriminator(x => x.Tool)
            .HasValue<FluidAttacksFinding>(SarifTool.FluidAttacks)
            .HasValue<GitLeaksFinding>(SarifTool.GitLeaks)
            .HasValue<OwaspDependencyCheckFinding>(SarifTool.OwaspDependencyCheck)
            .HasValue<SemgrepFinding>(SarifTool.Semgrep)
            .HasValue<TrivyFinding>(SarifTool.Trivy)
            .HasValue<OwaspZapFinding>(SarifTool.OwaspZap);
    }
}
