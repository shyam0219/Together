namespace CommunityOS.Api.DTOs;

public sealed record PollOptionDto(
    Guid PollOptionId,
    string Text,
    int SortOrder,
    int VoteCount
);

public sealed record PollDto(
    Guid PollId,
    Guid PostId,
    string Question,
    IReadOnlyList<PollOptionDto> Options,
    int TotalVotes,
    Guid? MyVotedOptionId
);

public sealed record CreatePollRequest(string Question, List<string> Options);

public sealed record VoteRequest(Guid OptionId);
