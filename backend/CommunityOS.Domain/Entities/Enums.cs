namespace CommunityOS.Domain.Entities;

public enum UserStatus
{
    Active = 0,
    Suspended = 1,
    Banned = 2
}

public enum UserRole
{
    PlatformOwner = 0,
    TenantOwner = 1,
    Admin = 2,
    Moderator = 3,
    Member = 4,
    Guest = 5
}

public enum PostStatus
{
    Active = 0,
    Hidden = 1,
    Removed = 2
}

public enum GroupVisibility
{
    Public = 0,
    Private = 1
}

public enum GroupMemberRole
{
    Member = 0,
    Moderator = 1
}

public enum ReportTargetType
{
    Post = 0,
    Comment = 1,
    User = 2
}

public enum ReportStatus
{
    Open = 0,
    Reviewed = 1,
    Actioned = 2
}

public enum NotificationType
{
    Mention = 0
}

public enum ReactionType
{
    Like = 0
}
