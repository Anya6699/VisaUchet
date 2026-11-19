namespace VisaUchet.Domain;

// Limits taken from the Visa Rules and the SRS (form tables 4.1, 4.2)
public static class DomainConstants
{
    public const int NameMaxLength = 40;
    public const int PassportMinLength = 5;
    public const int PassportMaxLength = 9;
    public const int CitizenshipCodeLength = 3;
    public const int PassportReserveDays = 90;
    public const int TransitMaxValidityDays = 365;
    public const int ShortTermMaxValidityDays = 90;
    public const int LongTermMinValidityDays = 91;
    public const int LongTermMaxValidityDays = 5 * 365;
    public const int UrgentReviewWorkingDays = 2;
    public const int RegularReviewWorkingDays = 5;
    public const int CommentMinLength = 10;
    public const int CommentMaxLength = 500;
    public const decimal FeeMax = 999.99m;
    public const string VisaNumberPrefix = "BLR";
    public const int VisaNumberDigits = 7;
    public const string ApplicationNumberPrefix = "VZ";
    public const int ApplicationNumberDigits = 6;

    public static readonly IReadOnlyList<string> Currencies = ["EUR", "USD", "BYN"];

    // Visa categories as written on the visa (Visa Rules, item 12)
    public static readonly IReadOnlyList<string> Categories =
    [
        "дыпламатычная", "службовая", "дзелавыя стасункі", "спорт/культура/навука/адукацыя",
        "гуманітарная дапамога", "рэлігійныя стасункі", "прэса", "прыватная", "вучоба",
        "праца", "турызм", "на пастаяннае пражыванне",
    ];
}
