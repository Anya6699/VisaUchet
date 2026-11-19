namespace VisaUchet.Domain;

// Record of the journal of issued visas (SRS entity «Выданная виза», JRN-01 – JRN-05)
public sealed class IssuedVisa
{
    public required string VisaNumber { get; init; }

    // Null after the application is deleted at the end of its retention period (JRN-05)
    public string? ApplicationNumber { get; init; }

    public DateOnly IssueDate { get; init; }

    public required string HolderName { get; init; }

    public required string HolderCitizenship { get; init; }

    public required VisaParameters Parameters { get; init; }

    public decimal Fee { get; init; }

    public required string Currency { get; init; }

    public required string IssuedBy { get; init; }

    public VisaState State { get; private set; } = VisaState.Valid;

    public DateOnly? CancelDate { get; private set; }

    public string? CancelReason { get; private set; }

    // JRN-03: the record is kept and marked as cancelled
    public void Cancel(string reason, DateOnly date)
    {
        if (State == VisaState.Cancelled)
        {
            throw new DomainException($"Виза {VisaNumber} уже аннулирована.");
        }

        State = VisaState.Cancelled;
        CancelDate = date;
        CancelReason = reason;
    }

    // Restores a cancelled record from storage
    public void LoadCancellation(DateOnly date, string reason)
    {
        State = VisaState.Cancelled;
        CancelDate = date;
        CancelReason = reason;
    }
}
