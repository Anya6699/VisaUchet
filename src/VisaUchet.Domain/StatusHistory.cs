namespace VisaUchet.Domain;

// One record of the status history; never changed after creation (STS-04)
public sealed class StatusHistory
{
    public int Id { get; set; }

    public required string ApplicationNumber { get; init; }

    // Null for the first record created at registration
    public ApplicationStatus? OldStatus { get; init; }

    public ApplicationStatus NewStatus { get; init; }

    public DateTime ChangedAt { get; init; }

    public required string EmployeeLogin { get; init; }

    public string? Comment { get; init; }
}
