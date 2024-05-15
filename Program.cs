using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Security Dashboard Backend");

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    // Add Serilog
    builder.Host.UseSerilog((context, serviceProvider, config) =>
    {
        config.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(serviceProvider)
            .Enrich.FromLogContext();
    });

    // Add DbContext for Dependency Injection using the Connection String from appsettings.json
    builder.Services.AddDbContext<SecDashboardContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SecDashboard")));

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options => { options.IncludeQueryInRequestPath = true; });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthorization();

    app.MapControllers();

    // Create and migrate the database before running the app
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<SecDashboardContext>();

        var pendingMigrations = context.Database.GetPendingMigrations().ToList();

        if (pendingMigrations.Any())
        {
            Log.Information("Database has {MigrationCount} migration(s) pending: {PendingMigrations}", pendingMigrations.Count, pendingMigrations);

            context.Database.Migrate();

            Log.Information("Pending database migrations have been applied.");
        }
        else
        {
            Log.Information("Database is up to date. No pending migrations found.");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
