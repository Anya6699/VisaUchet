using System.Text.RegularExpressions;

namespace VisaUchet.Domain;

// STS-03, VIS-02: issued visa may not be wider than requested; blank number is unique
public static partial class VisaIssueValidator
{
    public static ValidationResult Validate(VisaIssueInput visa, VisaParameters requested, bool numberExists)
    {
        ArgumentNullException.ThrowIfNull(visa);
        ArgumentNullException.ThrowIfNull(requested);
        var result = new ValidationResult();
        CheckNumber(visa.VisaNumber, numberExists, result);
        CheckPeriod(visa, requested, result);
        CheckFee(visa, result);
        return result;
    }

    private static void CheckNumber(string? number, bool numberExists, ValidationResult result)
    {
        if (!VisaNumberPattern().IsMatch(number ?? string.Empty))
        {
            result.Add(Fields.VisaNumber, $"Номер бланка: {DomainConstants.VisaNumberPrefix} и {DomainConstants.VisaNumberDigits} цифр.");
            return;
        }

        result.Require(!numberExists, Fields.VisaNumber, $"Бланк {number} уже использован.");
    }

    private static void CheckPeriod(VisaIssueInput visa, VisaParameters requested, ValidationResult result)
    {
        var issued = new VisaParameters(
            requested.Type, requested.Category, visa.Multiplicity, visa.ValidFrom, visa.ValidTo, visa.StayDays);
        result.Require(visa.ValidTo >= visa.ValidFrom, Fields.ValidTo, "Дата окончания не может быть раньше даты начала.");
        result.Require(
            issued.IsWithin(requested),
            Fields.ValidFrom,
            $"Период визы должен быть внутри запрошенного: {requested.ValidFrom:dd.MM.yyyy} – {requested.ValidTo:dd.MM.yyyy}.");
        result.Require(visa.Multiplicity <= requested.Multiplicity, Fields.Multiplicity, "Кратность не может быть больше запрошенной.");
        result.Require(
            visa.StayDays >= 1 && visa.StayDays <= requested.StayDays && visa.StayDays <= issued.PeriodDays,
            Fields.StayDays,
            $"Срок пребывания – от 1 до {Math.Min(requested.StayDays, Math.Max(issued.PeriodDays, 1))} суток.");
    }

    private static void CheckFee(VisaIssueInput visa, ValidationResult result)
    {
        result.Require(visa.Fee >= 0 && visa.Fee <= DomainConstants.FeeMax, Fields.Fee, $"Сбор – от 0 до {DomainConstants.FeeMax}.");
        result.Require(decimal.Round(visa.Fee, 2) == visa.Fee, Fields.Fee, "Сбор указывается с точностью до 2 знаков.");
        result.Require(DomainConstants.Currencies.Contains(visa.Currency), Fields.Currency, "Выберите валюту: EUR, USD или BYN.");
    }

    [GeneratedRegex("^BLR[0-9]{7}$")]
    private static partial Regex VisaNumberPattern();
}
