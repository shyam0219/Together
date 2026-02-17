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

        var tenantProvider = services.GetRequiredService<ITenantProvider>();

        // Ensure tenant context is always set (query filters reference CurrentTenantId even when PlatformOwner).
        var seTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        tenantProvider.Set(seTenantId, isPlatformOwner: true);

        var db = services.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying EF Core migrations...");
        await db.Database.MigrateAsync(ct);

        // Seed tenants
        if (!await db.Tenants.AnyAsync(ct))
        {
            logger.LogInformation("Seeding tenants...");
            db.Tenants.AddRange(
                new CommunityOS.Domain.Entities.Tenant { TenantId = seTenantId, Code = "SE", Name = "Sweden" },
                new CommunityOS.Domain.Entities.Tenant { TenantId = itTenantId, Code = "IT", Name = "Italy" }
            );

            await db.SaveChangesAsync(ct);
        }

        // Seed users (PlatformOwner + per-tenant Admin + 2 Members per tenant)
        if (!await db.Users.AnyAsync(ct))
        {
            logger.LogInformation("Seeding users...");
            var hasher = services.GetRequiredService<IPasswordHasher>();

            // PlatformOwner (special: still has a TenantId, set to SE for simplicity; role bypasses filters anyway)
            tenantProvider.Set(seTenantId, isPlatformOwner: true);
            db.Users.Add(new CommunityOS.Domain.Entities.User
            {
                UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Email = "owner@platform.local",
                PasswordHash = hasher.Hash("Password123!"),
                FirstName = "Platform",
                LastName = "Owner",
                Role = CommunityOS.Domain.Entities.UserRole.PlatformOwner,
                Status = CommunityOS.Domain.Entities.UserStatus.Active,
            });

            // Sweden Admin + Members
            tenantProvider.Set(seTenantId, isPlatformOwner: false);
            db.Users.Add(new CommunityOS.Domain.Entities.User
            {
                UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Email = "admin.se@community.local",
                PasswordHash = hasher.Hash("Password123!"),
                FirstName = "Sweden",
                LastName = "Admin",
                Role = CommunityOS.Domain.Entities.UserRole.Admin,
                Status = CommunityOS.Domain.Entities.UserStatus.Active,
            });
            db.Users.Add(new CommunityOS.Domain.Entities.User
            {
                UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb01"),
                Email = "member1.se@community.local",
                PasswordHash = hasher.Hash("Password123!"),
                FirstName = "Sweden",
                LastName = "Member1",
                Role = CommunityOS.Domain.Entities.UserRole.Member,
                Status = CommunityOS.Domain.Entities.UserStatus.Active,
            });
            db.Users.Add(new CommunityOS.Domain.Entities.User
            {
                UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb02"),
                Email = "member2.se@community.local",
                PasswordHash = hasher.Hash("Password123!"),
                FirstName = "Sweden",
                LastName = "Member2",
                Role = CommunityOS.Domain.Entities.UserRole.Member,
                Status = CommunityOS.Domain.Entities.UserStatus.Active,
            });

            // Italy Admin + Members
            tenantProvider.Set(itTenantId, isPlatformOwner: false);
            db.Users.Add(new CommunityOS.Domain.Entities.User
            {
                UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Email = "admin.it@community.local",
                PasswordHash = hasher.Hash("Password123!"),
                FirstName = "Italy",
                LastName = "Admin",
                Role = CommunityOS.Domain.Entities.UserRole.Admin,
                Status = CommunityOS.Domain.Entities.UserStatus.Active,
            });
            db.Users.Add(new CommunityOS.Domain.Entities.User
            {
                UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01"),
                Email = "member1.it@community.local",
                PasswordHash = hasher.Hash("Password123!"),
                FirstName = "Italy",
                LastName = "Member1",
                Role = CommunityOS.Domain.Entities.UserRole.Member,
                Status = CommunityOS.Domain.Entities.UserStatus.Active,
            });
            db.Users.Add(new CommunityOS.Domain.Entities.User
            {
                UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02"),
                Email = "member2.it@community.local",
                PasswordHash = hasher.Hash("Password123!"),
                FirstName = "Italy",
                LastName = "Member2",
                Role = CommunityOS.Domain.Entities.UserRole.Member,
                Status = CommunityOS.Domain.Entities.UserStatus.Active,
            });

            await db.SaveChangesAsync(ct);
        }
    }
}
