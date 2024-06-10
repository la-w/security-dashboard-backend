using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend;
using SecurityDashboard_Backend.Authentication;
using SecurityDashboard_Backend.Authentication.AuthorizationHandlers;
using SecurityDashboard_Backend.Utilities;
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
    builder.Services.Configure<ApiKeyConfiguration>(builder.Configuration.GetSection("ApiKey"));

    builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
        .AddApiKeyInAuthorizationHeader<ApiKeyProvider>(options =>
        {
            options.KeyName = "X-API-KEY";
            options.Realm = "Security Dashboard API";
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(Policies.UsedApiKey, policy => policy.AddRequirements(new UsedApiKeyRequirement()));
    });

    builder.Services.AddSingleton<IAuthorizationHandler, UsedApiKeyAuthorizationHandler>();

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

    // Add Remote-IP filter
    builder.Services.AddRemoteIpCheckActionFilter(builder.Configuration.GetSection("RemoteIpWhitelist").Get<string[]>());

    // Register Controllers and add SARIF Input/Output Formatters as highest priority
    builder.Services.AddControllers(options =>
    {
        options.InputFormatters.Insert(0, new SarifInputFormatter());
        options.OutputFormatters.Insert(0, new SarifOutputFormatter());
    });
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure forwarded headers
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    var app = builder.Build();

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options => { options.IncludeQueryInRequestPath = true; });

    // Map remote IPs from proxies
    app.UseForwardedHeaders();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        Log.Warning("Environment is set to Development. Enabling Swagger UI and Swagger Endpoints. *DO NOT USE FOR PRODUCTION!*");
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication();
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
