using System.Text.RegularExpressions;

namespace CommunityOS.Api.Services;

public static class MentionParser
{
    private static readonly Regex MentionRegex = new("(?<!\\w)@([a-zA-Z0-9._-]{2,50})", RegexOptions.Compiled);
    private static readonly Regex HashtagRegex = new("(?<!\\w)#([a-zA-Z0-9_]{2,50})", RegexOptions.Compiled);

    public static IReadOnlyCollection<string> ExtractMentions(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return MentionRegex.Matches(text).Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static IReadOnlyCollection<string> ExtractHashtags(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return HashtagRegex.Matches(text).Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
