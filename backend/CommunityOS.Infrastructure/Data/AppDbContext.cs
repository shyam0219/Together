using CommunityOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Infrastructure.Data;

public interface ITenantContext
{
    Guid CurrentTenantId { get; }
    bool HasTenant { get; }
    bool IsPlatformOwner { get; }
}

public sealed class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostImage> PostImages => Set<PostImage>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant is NOT tenant-scoped.
        modelBuilder.Entity<Tenant>(b =>
        {
            b.HasKey(x => x.TenantId);
            b.Property(x => x.Code).HasMaxLength(8).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
        });

        ConfigureUser(modelBuilder);
        ConfigurePost(modelBuilder);
        ConfigurePostImage(modelBuilder);
        ConfigureComment(modelBuilder);
        ConfigureReaction(modelBuilder);
        ConfigureBookmark(modelBuilder);
        ConfigureGroup(modelBuilder);
        ConfigureGroupMember(modelBuilder);
        ConfigureReport(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureAuditLog(modelBuilder);

        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // Important: platform owner bypasses tenant filtering.
        // Uses captured scoped ITenantProvider. EF Core evaluates this per context instance.
        Guid CurrentTenantId() => _tenantContext.CurrentTenantId;
        bool IsPlatformOwner() => _tenantContext.IsPlatformOwner;

        modelBuilder.Entity<User>().HasQueryFilter(x => IsPlatformOwner() || x.TenantId == CurrentTenantId());
        modelBuilder.Entity<Post>().HasQueryFilter(x => (IsPlatformOwner() || x.TenantId == CurrentTenantId()) && x.SoftDeletedAt == null);
        modelBuilder.Entity<PostImage>().HasQueryFilter(x => IsPlatformOwner() || x.TenantId == CurrentTenantId());
        modelBuilder.Entity<Comment>().HasQueryFilter(x => (IsPlatformOwner() || x.TenantId == CurrentTenantId()) && x.SoftDeletedAt == null);
        modelBuilder.Entity<Reaction>().HasQueryFilter(x => IsPlatformOwner() || x.TenantId == CurrentTenantId());
        modelBuilder.Entity<Bookmark>().HasQueryFilter(x => IsPlatformOwner() || x.TenantId == CurrentTenantId());
        modelBuilder.Entity<Group>().HasQueryFilter(x => IsPlatformOwner() || x.TenantId == CurrentTenantId());
        modelBuilder.Entity<GroupMember>().HasQueryFilter(x => IsPlatformOwner() || x.TenantId == CurrentTenantId());
        modelBuilder.Entity<Report>().HasQueryFilter(x => IsPlatformOwner() || x.TenantId == CurrentTenantId());
        modelBuilder.Entity<Notification>().HasQueryFilter(x => IsPlatformOwner() || x.TenantId == CurrentTenantId());
        modelBuilder.Entity<AuditLog>().HasQueryFilter(x => IsPlatformOwner() || x.TenantId == CurrentTenantId());
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantAndTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantAndTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantAndTimestamps()
    {
        // Tenant must be resolved for any write to tenant-scoped entities.
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (!_tenantProvider.HasTenant)
                {
                    throw new InvalidOperationException("Tenant is not set; cannot create tenant-scoped entity.");
                }

                entry.Entity.TenantId = _tenantProvider.CurrentTenantId;
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;

                // Ensure GUID PKs are not empty if used as key
            }
            else if (entry.State == EntityState.Modified)
            {
                // Prevent cross-tenant updates
                if (!_tenantProvider.IsPlatformOwner && entry.Entity.TenantId != _tenantProvider.CurrentTenantId)
                {
                    throw new InvalidOperationException("Cross-tenant update is not allowed.");
                }

                entry.Entity.UpdatedAt = now;
            }
        }

        // Soft-delete timestamp update happens in controllers/services.
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.UserId);
            b.Property(x => x.Email).HasMaxLength(320).IsRequired();
            b.Property(x => x.PasswordHash).HasMaxLength(400).IsRequired();
            b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            b.Property(x => x.City).HasMaxLength(120);
            b.Property(x => x.Bio).HasMaxLength(1000);
            b.Property(x => x.AvatarUrl).HasMaxLength(500);

            // Unique within tenant
            b.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
        });
    }

    private static void ConfigurePost(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>(b =>
        {
            b.HasKey(x => x.PostId);
            b.Property(x => x.BodyText).HasMaxLength(5000).IsRequired();
            b.Property(x => x.LinkUrl).HasMaxLength(2048);
            b.Property(x => x.LinkTitle).HasMaxLength(400);
            b.Property(x => x.LinkDescription).HasMaxLength(2000);
            b.Property(x => x.LinkImageUrl).HasMaxLength(2048);

            b.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.Images).WithOne(x => x.Post).HasForeignKey(x => x.PostId);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.AuthorId, x.CreatedAt });
        });
    }

    private static void ConfigurePostImage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostImage>(b =>
        {
            b.HasKey(x => x.PostImageId);
            b.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
        });
    }

    private static void ConfigureComment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>(b =>
        {
            b.HasKey(x => x.CommentId);
            b.Property(x => x.Text).HasMaxLength(2000).IsRequired();

            b.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.ParentComment)
                .WithMany(x => x!.Replies)
                .HasForeignKey(x => x.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.PostId, x.CreatedAt });
        });
    }

    private static void ConfigureReaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reaction>(b =>
        {
            b.HasKey(x => x.ReactionId);
            b.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.UserId, x.PostId }).IsUnique();
        });
    }

    private static void ConfigureBookmark(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bookmark>(b =>
        {
            b.HasKey(x => x.BookmarkId);
            b.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.UserId, x.PostId }).IsUnique();
        });
    }

    private static void ConfigureGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(b =>
        {
            b.HasKey(x => x.GroupId);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);

            b.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.Name });
            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
        });
    }

    private static void ConfigureGroupMember(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupMember>(b =>
        {
            b.HasKey(x => x.GroupMemberId);
            b.HasOne(x => x.Group).WithMany(x => x.Members).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.GroupId, x.UserId }).IsUnique();
        });
    }

    private static void ConfigureReport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Report>(b =>
        {
            b.HasKey(x => x.ReportId);
            b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
        });
    }

    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(b =>
        {
            b.HasKey(x => x.NotificationId);
            b.Property(x => x.PayloadJson).IsRequired();
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.UserId, x.CreatedAt });
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasKey(x => x.AuditLogId);
            b.Property(x => x.ActionType).HasMaxLength(200).IsRequired();
            b.Property(x => x.TargetType).HasMaxLength(200).IsRequired();
            b.Property(x => x.MetadataJson).IsRequired();

            b.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
        });
    }
}
