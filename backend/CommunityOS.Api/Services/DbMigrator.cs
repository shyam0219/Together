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
        logger.LogInformation("Ensuring seed users...");
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var defaultPasswordHash = hasher.Hash("Password123!");

        async Task EnsureUserAsync(Guid userId, string email, Guid expectedTenantId, string firstName, string lastName, CommunityOS.Domain.Entities.UserRole role)
        {
            // PlatformOwner context to bypass filters and allow tenant correction.
            tenantProvider.Set(seTenantId, isPlatformOwner: true);

            var emailNorm = email.Trim().ToLowerInvariant();
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == emailNorm, ct);

            if (existing is null)
            {
                // Switch to expected tenant for creation (auto TenantId on SaveChanges)
                tenantProvider.Set(expectedTenantId, isPlatformOwner: role == CommunityOS.Domain.Entities.UserRole.PlatformOwner);

                db.Users.Add(new CommunityOS.Domain.Entities.User
                {
                    UserId = userId,
                    Email = emailNorm,
                    PasswordHash = defaultPasswordHash,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    Status = CommunityOS.Domain.Entities.UserStatus.Active,
                });

                await db.SaveChangesAsync(ct);
                return;
            }

            // Update/correct
            tenantProvider.Set(seTenantId, isPlatformOwner: true);

            var changed = false;
            if (existing.UserId != userId)
            {
                // Keep DB id stable if it already exists; do not change PK.
            }
            if (existing.TenantId != expectedTenantId)
            {
                existing.TenantId = expectedTenantId;
                changed = true;
            }
            if (existing.Role != role)
            {
                existing.Role = role;
                changed = true;
            }
            if (existing.Status != CommunityOS.Domain.Entities.UserStatus.Active)
            {
                existing.Status = CommunityOS.Domain.Entities.UserStatus.Active;
                changed = true;
            }
            if (existing.FirstName != firstName)
            {
                existing.FirstName = firstName;
                changed = true;
            }
            if (existing.LastName != lastName)
            {
                existing.LastName = lastName;
                changed = true;
            }
            if (existing.PasswordHash != defaultPasswordHash)
            {
                existing.PasswordHash = defaultPasswordHash;
                changed = true;
            }

            if (changed)
            {
                await db.SaveChangesAsync(ct);
            }
        }

        await EnsureUserAsync(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "owner@platform.local",
            seTenantId,
            "Platform",
            "Owner",
            CommunityOS.Domain.Entities.UserRole.PlatformOwner
        );

        // Sweden
        await EnsureUserAsync(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "admin.se@community.local",
            seTenantId,
            "Sweden",
            "Admin",
            CommunityOS.Domain.Entities.UserRole.Admin
        );
        await EnsureUserAsync(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb01"),
            "member1.se@community.local",
            seTenantId,
            "Sweden",
            "Member1",
            CommunityOS.Domain.Entities.UserRole.Member
        );
        await EnsureUserAsync(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb02"),
            "member2.se@community.local",
            seTenantId,
            "Sweden",
            "Member2",
            CommunityOS.Domain.Entities.UserRole.Member
        );

        // Italy
        await EnsureUserAsync(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "admin.it@community.local",
            itTenantId,
            "Italy",
            "Admin",
            CommunityOS.Domain.Entities.UserRole.Admin
        );
        await EnsureUserAsync(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01"),
            "member1.it@community.local",
            itTenantId,
            "Italy",
            "Member1",
            CommunityOS.Domain.Entities.UserRole.Member
        );
        await EnsureUserAsync(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02"),
            "member2.it@community.local",
            itTenantId,
            "Italy",
            "Member2",
            CommunityOS.Domain.Entities.UserRole.Member
        );
    }
}
