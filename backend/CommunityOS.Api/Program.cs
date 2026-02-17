using System.Text;
using CommunityOS.Api.Middleware;
using CommunityOS.Api.Services;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Tenant context (resolved from JWT by middleware)
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<ITenantContext, EfTenantContextAdapter>();

// EF Core: SQL Server is primary; fallback to SQLite when SQL Server is unavailable.
var sqlServerConnStr = builder.Configuration.GetConnectionString("SqlServer") ?? string.Empty;
var sqliteConnStr = builder.Configuration.GetSection("Fallback")["SqliteFile"] ?? "Data Source=/app/backend/communityos-dev.sqlite";

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(sqlServerConnStr) && DbProviderSelector.CanConnectToSqlServer(sqlServerConnStr))
    {
        options.UseSqlServer(sqlServerConnStr);
    }
    else
    {
        // In-environment sanity checks
        options.UseSqlite(sqliteConnStr);
    }
});

// JWT auth
var jwtKey = builder.Configuration["Auth:JwtSigningKey"] ?? "dev-only-insecure-change-me";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Root endpoint for infra probes.
app.MapGet("/", () => Results.Text("CommunityOS API running", "text/plain"))
   .WithName("Root");

// Basic health check for supervisor + curl sanity.
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health");

// Ensure DB can start (migrations + basic seeds)
await DbMigrator.MigrateAndSeedAsync(app.Services);

app.Run();
