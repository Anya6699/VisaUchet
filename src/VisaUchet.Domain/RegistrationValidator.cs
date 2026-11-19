using System.Text.RegularExpressions;

namespace VisaUchet.Domain;

// REG-02, REG-06: checks of form Ф1 fields (SRS table 4.1)
public static partial class RegistrationValidator
{
    // Full check used by the service: field data (REG-02) and passport term (REG-06)
    public static ValidationResult Validate(ApplicationInput input, DateOnly today)
    {
        var result = ValidateData(input, today);
        CheckPassportTerm(input, result);
        return result;
    }

    // REG-02: «Проверить данные заявки» – errors are shown next to the fields and the operator corrects them
    public static ValidationResult ValidateData(ApplicationInput input, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = new ValidationResult();
        CheckPerson(input, today, result);
        CheckPassportNumber(input, result);
        CheckVisa(input, today, result);
        return result;
    }

    // REG-06: «Проверить срок действия паспорта» – passport valid at least 90 days after the visa ends
    public static bool IsPassportTermSufficient(ApplicationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.PassportExpiry >= RequiredPassportExpiry(input);
    }

    public static DateOnly RequiredPassportExpiry(ApplicationInput input) =>
        input.ValidTo.AddDays(DomainConstants.PassportReserveDays);

    private static void CheckPerson(ApplicationInput input, DateOnly today, ValidationResult result)
    {
        CheckLatinName(input.LastName, Fields.LastName, "Фамилия", result);
        CheckLatinName(input.FirstName, Fields.FirstName, "Имя", result);
        result.Require(input.BirthDate <= today, Fields.BirthDate, "Дата рождения не может быть позже текущей даты.");
        result.Require(Countries.IsKnown(input.Citizenship), Fields.Citizenship, "Выберите гражданство из списка ISO 3166-1.");
    }

    private static void CheckPassportNumber(ApplicationInput input, ValidationResult result) =>
        result.Require(
            PassportPattern().IsMatch(input.PassportNumber ?? string.Empty),
            Fields.PassportNumber,
            $"Номер паспорта: {DomainConstants.PassportMinLength}–{DomainConstants.PassportMaxLength} латинских букв и цифр.");

    private static void CheckPassportTerm(ApplicationInput input, ValidationResult result) =>
        result.Require(
            IsPassportTermSufficient(input),
            Fields.PassportExpiry,
            $"Паспорт должен действовать не менее {DomainConstants.PassportReserveDays} суток после окончания визы (до {RequiredPassportExpiry(input):dd.MM.yyyy}).");

    private static void CheckVisa(ApplicationInput input, DateOnly today, ValidationResult result)
    {
        result.Require(DomainConstants.Categories.Contains(input.Category), Fields.Category, "Выберите категорию визы из списка.");
        result.Require(
            input.VisaType != VisaType.D || input.Multiplicity == Multiplicity.Multiple,
            Fields.Multiplicity,
            "Виза типа D выдаётся только многократной.");
        result.Require(input.ValidFrom >= today, Fields.ValidFrom, "Дата начала не может быть раньше текущей даты.");
        if (input.ValidTo <= input.ValidFrom)
        {
            result.Add(Fields.ValidTo, "Дата окончания должна быть позже даты начала.");
            return;
        }

        CheckPeriod(input.ToParameters(), result);
    }

    private static void CheckPeriod(VisaParameters visa, ValidationResult result)
    {
        var (min, max) = PeriodLimits(visa.Type);
        result.Require(
            visa.PeriodDays >= min && visa.PeriodDays <= max,
            Fields.ValidTo,
            $"Для визы типа {visa.Type} срок действия – от {min} до {max} суток.");
        result.Require(
            visa.StayDays >= 1 && visa.StayDays <= visa.PeriodDays,
            Fields.StayDays,
            $"Срок пребывания – от 1 до {visa.PeriodDays} суток.");
    }

    public static (int Min, int Max) PeriodLimits(VisaType type) => type switch
    {
        VisaType.B => (1, DomainConstants.TransitMaxValidityDays),
        VisaType.C => (1, DomainConstants.ShortTermMaxValidityDays),
        _ => (DomainConstants.LongTermMinValidityDays, DomainConstants.LongTermMaxValidityDays),
    };

    private static void CheckLatinName(string? value, string field, string title, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.Add(field, $"{title}: обязательное поле.");
            return;
        }

        result.Require(value.Length <= DomainConstants.NameMaxLength, field, $"{title}: не длиннее {DomainConstants.NameMaxLength} символов.");
        result.Require(LatinNamePattern().IsMatch(value), field, $"{title}: только латинские буквы, пробел и дефис.");
    }

    [GeneratedRegex("^[A-Za-z]+([ -][A-Za-z]+)*$")]
    private static partial Regex LatinNamePattern();

    [GeneratedRegex("^[A-Za-z0-9]{5,9}$")]
    private static partial Regex PassportPattern();
}
