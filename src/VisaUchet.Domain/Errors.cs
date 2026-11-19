namespace VisaUchet.Domain;

// Field-level validation error shown next to the input field (UI-03)
public sealed record FieldError(string Field, string Message);

// Business rule violation; carries all field errors at once
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
        Errors = [new FieldError(string.Empty, message)];
    }

    public DomainException(IReadOnlyList<FieldError> errors)
        : base(string.Join("; ", errors.Select(e => e.Message)))
    {
        Errors = errors;
    }

    public IReadOnlyList<FieldError> Errors { get; }
}

// Result of an operation that can fail with field errors
public sealed class ValidationResult
{
    private readonly List<FieldError> _errors = [];

    public bool IsValid => _errors.Count == 0;

    public IReadOnlyList<FieldError> Errors => _errors;

    public void Add(string field, string message) => _errors.Add(new FieldError(field, message));

    public void Require(bool condition, string field, string message)
    {
        if (!condition)
        {
            Add(field, message);
        }
    }

    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new DomainException(_errors);
        }
    }
}
