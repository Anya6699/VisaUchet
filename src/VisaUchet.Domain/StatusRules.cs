namespace VisaUchet.Domain;

// Allowed status transitions (SRS table 3.2, requirement STS-01)
public sealed class StatusRules
{
    private static readonly IReadOnlyDictionary<ApplicationStatus, ApplicationStatus[]> Transitions =
        new Dictionary<ApplicationStatus, ApplicationStatus[]>
        {
            [ApplicationStatus.Registered] =
                [ApplicationStatus.InReview, ApplicationStatus.Returned, ApplicationStatus.Withdrawn],
            [ApplicationStatus.InReview] =
            [
                ApplicationStatus.Interview, ApplicationStatus.OnApproval, ApplicationStatus.Issued,
                ApplicationStatus.Refused, ApplicationStatus.Withdrawn,
            ],
            [ApplicationStatus.Interview] = [ApplicationStatus.InReview],
            [ApplicationStatus.OnApproval] = [ApplicationStatus.InReview],
            [ApplicationStatus.Issued] = [ApplicationStatus.Cancelled],
        };

    // STS-02: these transitions need a comment of 10–500 characters
    private static readonly HashSet<ApplicationStatus> CommentRequired =
        [ApplicationStatus.Returned, ApplicationStatus.Cancelled];

    private static readonly HashSet<ApplicationStatus> Decisions =
        [ApplicationStatus.Issued, ApplicationStatus.Refused];

    public static StatusRules Default { get; } = new();

    public IReadOnlyList<ApplicationStatus> AllowedFrom(ApplicationStatus status) =>
        Transitions.TryGetValue(status, out var next) ? next : [];

    public bool IsFinal(ApplicationStatus status) => AllowedFrom(status).Count == 0;

    public bool CanMove(ApplicationStatus from, ApplicationStatus to) => AllowedFrom(from).Contains(to);

    public bool RequiresComment(ApplicationStatus to) => CommentRequired.Contains(to);

    // Decision on the application: visa issued or refused
    public bool IsDecision(ApplicationStatus status) => Decisions.Contains(status);

    // Application is still waiting for a decision
    public bool IsPending(ApplicationStatus status) =>
        status is ApplicationStatus.Registered or ApplicationStatus.InReview
            or ApplicationStatus.Interview or ApplicationStatus.OnApproval;
}
