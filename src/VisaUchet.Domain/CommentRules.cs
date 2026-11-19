namespace VisaUchet.Domain;

// STS-02: comment length rules for a status change
public static class CommentRules
{
    public const string Field = "Comment";

    public static ValidationResult Check(ApplicationStatus to, string? comment)
    {
        var result = new ValidationResult();
        var length = comment?.Trim().Length ?? 0;
        if (StatusRules.Default.RequiresComment(to))
        {
            result.Require(
                length >= DomainConstants.CommentMinLength,
                Field,
                $"Комментарий обязателен: от {DomainConstants.CommentMinLength} до {DomainConstants.CommentMaxLength} символов.");
        }

        result.Require(
            length <= DomainConstants.CommentMaxLength,
            Field,
            $"Комментарий не длиннее {DomainConstants.CommentMaxLength} символов.");
        return result;
    }
}
