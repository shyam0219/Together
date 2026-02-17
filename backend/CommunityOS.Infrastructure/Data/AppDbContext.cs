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
    public DbSet<GroupPost> GroupPosts => Set<GroupPost>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Phase 1B
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();
    public DbSet<PollVote> PollVotes => Set<PollVote>();

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
        ConfigureGroupPost(modelBuilder);
        ConfigureReport(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureAuditLog(modelBuilder);

        // Phase 1B
        ConfigureConversation(modelBuilder);
        ConfigureConversationParticipant(modelBuilder);
        ConfigureMessage(modelBuilder);
        ConfigurePoll(modelBuilder);
        ConfigurePollOption(modelBuilder);
        ConfigurePollVote(modelBuilder);

        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // Important: platform owner bypasses tenant filtering.
        // Use DbContext instance members directly (no local functions in expression trees).
        modelBuilder.Entity<User>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<Post>().HasQueryFilter(x => (_tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId) && x.SoftDeletedAt == null);
        modelBuilder.Entity<PostImage>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<Comment>().HasQueryFilter(x => (_tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId) && x.SoftDeletedAt == null);
        modelBuilder.Entity<Reaction>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<Bookmark>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<Group>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<GroupMember>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<GroupPost>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<Report>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<Notification>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);

        modelBuilder.Entity<Conversation>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<ConversationParticipant>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<Message>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);

        modelBuilder.Entity<Poll>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<PollOption>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<PollVote>().HasQueryFilter(x => _tenantContext.IsPlatformOwner || x.TenantId == _tenantContext.CurrentTenantId);
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
                if (!_tenantContext.HasTenant)
                {
                    throw new InvalidOperationException("Tenant is not set; cannot create tenant-scoped entity.");
                }

                entry.Entity.TenantId = _tenantContext.CurrentTenantId;
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;

                // Ensure GUID PKs are not empty if used as key
            }
            else if (entry.State == EntityState.Modified)
            {
                // Prevent cross-tenant updates
                if (!_tenantContext.IsPlatformOwner && entry.Entity.TenantId != _tenantContext.CurrentTenantId)
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

    private static void ConfigureGroupPost(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupPost>(b =>
        {
            b.HasKey(x => x.GroupPostId);

            b.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.GroupId, x.PostId }).IsUnique();
        });
    }

    // ---- Phase 1B entity configuration ----
    private static void ConfigureConversation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(b =>
        {
            b.HasKey(x => x.ConversationId);

            b.HasMany(x => x.Participants).WithOne(x => x.Conversation).HasForeignKey(x => x.ConversationId);
            b.HasMany(x => x.Messages).WithOne(x => x.Conversation).HasForeignKey(x => x.ConversationId);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.DirectUserAId, x.DirectUserBId });
        });
    }

    private static void ConfigureConversationParticipant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConversationParticipant>(b =>
        {
            b.HasKey(x => x.ConversationParticipantId);

            b.HasOne(x => x.Conversation).WithMany(x => x.Participants).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.ConversationId, x.UserId }).IsUnique();
        });
    }

    private static void ConfigureMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(b =>
        {
            b.HasKey(x => x.MessageId);
            b.Property(x => x.BodyText).HasMaxLength(5000).IsRequired();

            b.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.ConversationId, x.SentAt });
            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
        });
    }

    private static void ConfigurePoll(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Poll>(b =>
        {
            b.HasKey(x => x.PollId);
            b.Property(x => x.Question).HasMaxLength(500).IsRequired();

            b.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Options).WithOne(x => x.Poll).HasForeignKey(x => x.PollId);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.PostId }).IsUnique();
        });
    }

    private static void ConfigurePollOption(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PollOption>(b =>
        {
            b.HasKey(x => x.PollOptionId);
            b.Property(x => x.Text).HasMaxLength(200).IsRequired();

            b.HasOne(x => x.Poll).WithMany(x => x.Options).HasForeignKey(x => x.PollId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Votes).WithOne(x => x.Option).HasForeignKey(x => x.PollOptionId);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.PollId, x.SortOrder });
        });
    }

    private static void ConfigurePollVote(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PollVote>(b =>
        {
            b.HasKey(x => x.PollVoteId);

            b.HasOne(x => x.Poll).WithMany().HasForeignKey(x => x.PollId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Option).WithMany(x => x.Votes).HasForeignKey(x => x.PollOptionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.PollId, x.UserId }).IsUnique();
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
