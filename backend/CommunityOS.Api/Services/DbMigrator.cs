using CommunityOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Services;

public static class DbMigrator
{
    public static async Task MigrateAndSeedAsync(IServiceProvider rootServices, CancellationToken ct = default)
    {
        using var scope = rootServices.CreateScope();

        // Use a special tenant context so DbContext can be created outside a request.
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbMigrator");

        // Replace ITenantContext for this scope only.
        var tenantProvider = services.GetRequiredService<ITenantProvider>();
        tenantProvider.Clear();

        // Create DbContext
        var db = services.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying EF Core migrations...");
        await db.Database.MigrateAsync(ct);

        // Seed tenants minimally (full seed comes in Milestone 2)
        if (!await db.Tenants.AnyAsync(ct))
        {
            logger.LogInformation("Seeding tenants...");
            db.Tenants.AddRange(
                new CommunityOS.Domain.Entities.Tenant { TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Code = "SE", Name = "Sweden" },
                new CommunityOS.Domain.Entities.Tenant { TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Code = "IT", Name = "Italy" }
            );

            await db.SaveChangesAsync(ct);
        }
    }
}
