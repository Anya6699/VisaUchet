namespace VisaUchet.Domain;

// ISO 3166-1 alpha-3 codes for citizenship; XXA – stateless person (ICAO Doc 9303)
public static class Countries
{
    private const string AllCodes =
        "ABW AFG AGO AIA ALA ALB AND ARE ARG ARM ASM ATA ATF ATG AUS AUT AZE BDI BEL BEN BES BFA BGD BGR " +
        "BHR BHS BIH BLM BLR BLZ BMU BOL BRA BRB BRN BTN BVT BWA CAF CAN CCK CHE CHL CHN CIV CMR COD COG " +
        "COK COL COM CPV CRI CUB CUW CXR CYM CYP CZE DEU DJI DMA DNK DOM DZA ECU EGY ERI ESH ESP EST ETH " +
        "FIN FJI FLK FRA FRO FSM GAB GBR GEO GGY GHA GIB GIN GLP GMB GNB GNQ GRC GRD GRL GTM GUF GUM GUY " +
        "HKG HMD HND HRV HTI HUN IDN IMN IND IOT IRL IRN IRQ ISL ISR ITA JAM JEY JOR JPN KAZ KEN KGZ KHM " +
        "KIR KNA KOR KWT LAO LBN LBR LBY LCA LIE LKA LSO LTU LUX LVA MAC MAF MAR MCO MDA MDG MDV MEX MHL " +
        "MKD MLI MLT MMR MNE MNG MNP MOZ MRT MSR MTQ MUS MWI MYS MYT NAM NCL NER NFK NGA NIC NIU NLD NOR " +
        "NPL NRU NZL OMN PAK PAN PCN PER PHL PLW PNG POL PRI PRK PRT PRY PSE PYF QAT REU ROU RUS RWA SAU " +
        "SDN SEN SGP SGS SHN SJM SLB SLE SLV SMR SOM SPM SRB SSD STP SUR SVK SVN SWE SWZ SXM SYC SYR TCA " +
        "TCD TGO THA TJK TKL TKM TLS TON TTO TUN TUR TUV TWN TZA UGA UKR UMI URY USA UZB VAT VCT VEN VGB " +
        "VIR VNM VUT WLF WSM YEM ZAF ZMB ZWE XXA";

    private static readonly IReadOnlyDictionary<string, string> RussianNames = new Dictionary<string, string>
    {
        ["AUT"] = "Австрия", ["ARM"] = "Армения", ["AZE"] = "Азербайджан", ["BEL"] = "Бельгия",
        ["BGR"] = "Болгария", ["BLR"] = "Беларусь", ["CAN"] = "Канада", ["CHE"] = "Швейцария",
        ["CHN"] = "Китай", ["CZE"] = "Чехия", ["DEU"] = "Германия", ["DNK"] = "Дания",
        ["ESP"] = "Испания", ["EST"] = "Эстония", ["FIN"] = "Финляндия", ["FRA"] = "Франция",
        ["GBR"] = "Великобритания", ["GEO"] = "Грузия", ["GRC"] = "Греция", ["HUN"] = "Венгрия",
        ["IND"] = "Индия", ["ISR"] = "Израиль", ["ITA"] = "Италия", ["JPN"] = "Япония",
        ["KAZ"] = "Казахстан", ["KGZ"] = "Кыргызстан", ["KOR"] = "Корея", ["LTU"] = "Литва",
        ["LVA"] = "Латвия", ["MDA"] = "Молдова", ["NLD"] = "Нидерланды", ["NOR"] = "Норвегия",
        ["POL"] = "Польша", ["PRT"] = "Португалия", ["ROU"] = "Румыния", ["RUS"] = "Россия",
        ["SRB"] = "Сербия", ["SVK"] = "Словакия", ["SWE"] = "Швеция", ["TJK"] = "Таджикистан",
        ["TKM"] = "Туркменистан", ["TUR"] = "Турция", ["UKR"] = "Украина", ["USA"] = "США",
        ["UZB"] = "Узбекистан", ["VNM"] = "Вьетнам", ["XXA"] = "Лицо без гражданства",
    };

    public static IReadOnlyList<string> Codes { get; } =
        AllCodes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static readonly HashSet<string> CodeSet = [.. Codes];

    public static bool IsKnown(string? code) => code is not null && CodeSet.Contains(code);

    public static string Display(string code) =>
        RussianNames.TryGetValue(code, out var name) ? $"{code} – {name}" : code;
}
