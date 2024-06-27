using System.Reflection;
using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecurityDashboard_Backend;
using SecurityDashboard_Backend.Authentication;
using SecurityDashboard_Backend.Services;
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
            options.KeyName = "ApiKey";
            options.Realm = "Security Dashboard API";
        });
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var oauthConfig = builder.Configuration.GetRequiredSection("OAuth").Get<OAuthConfig>();

            options.MetadataAddress = oauthConfig.MetadataAddress;
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidateIssuer = true,
                ValidateAudience = true,

                ValidIssuer = oauthConfig.Issuer,
                ValidAudience = oauthConfig.ClientId
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(Policies.UsedApiKey, policy =>
        {
            policy.AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser();
        });
        options.AddPolicy(Policies.OAuthUser,
            policy => policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
    });

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

    // Add CORS
    var allowedOriginsConfig = builder.Configuration.GetSection("AllowedOrigins");
    var allowedOrigins = allowedOriginsConfig.GetChildren()
        .Select(x => x.Value)
        .Where(x => x != null)
        .Cast<string>()
        .ToArray();


    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigin",
            policyBuilder =>
            {
                if (allowedOrigins.Length > 0)
                    policyBuilder.WithOrigins(allowedOrigins);
                else
                    policyBuilder.AllowAnyOrigin();

                policyBuilder.AllowAnyMethod().AllowAnyHeader().AllowCredentials();
            });
    });

    // Register Controllers and add SARIF Input/Output Formatters as highest priority
    builder.Services.AddControllers(options =>
    {
        options.InputFormatters.Insert(0, new SarifInputFormatter());
        options.OutputFormatters.Insert(0, new SarifOutputFormatter());
    });
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "TBD_PWL Dashboard Backend v1",
            Description = "API for the Security Dashboard Backend",
            Version = "1.0.0"
        });
        options.AddSecurityDefinition("apiKey", new()
        {
            Type = SecuritySchemeType.ApiKey,
            Scheme = "ApiKey",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "API-Key Authentication\n\n*Please prefix your API-Key with 'ApiKey ' (including the space) for it to work in SwaggerUI.*"
        });
        options.AddSecurityDefinition("oAuth", new()
        {
            Type = SecuritySchemeType.OpenIdConnect,
            OpenIdConnectUrl = new Uri(builder.Configuration.GetSection("OAuth").Get<OAuthConfig>().MetadataAddress),
            Description = "OAuth2 Authentication"
        });

        options.OperationFilter<SecurityRequirementsOperationFilter>();

        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
    });


    // Configure forwarded headers
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Add custom services
    builder.Services.AddScoped<ISarifImportService, SarifImportService>();

    var app = builder.Build();

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options => { options.IncludeQueryInRequestPath = true; });

    // Map remote IPs from proxies
    app.UseForwardedHeaders();

    // Use CORS
    app.UseCors("AllowSpecificOrigin");

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
