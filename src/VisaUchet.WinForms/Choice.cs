namespace VisaUchet.WinForms;

// Item of a drop-down list: stored value and displayed text
internal sealed record Choice<T>(T Value, string Text)
{
    public override string ToString() => Text;
}

internal static class Choices
{
    public static ComboBox Combo<T>(IEnumerable<Choice<T>> items, int width = 220)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width };
        combo.Items.AddRange(items.Cast<object>().ToArray());
        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }

        return combo;
    }

    public static T Selected<T>(ComboBox combo) =>
        combo.SelectedItem is Choice<T> choice
            ? choice.Value
            : throw new DomainException("Выберите значение из списка.");

    public static void Select<T>(ComboBox combo, T value)
    {
        var index = combo.Items.Cast<Choice<T>>().ToList().FindIndex(c => EqualityComparer<T>.Default.Equals(c.Value, value));
        if (index >= 0)
        {
            combo.SelectedIndex = index;
        }
    }

    public static IEnumerable<Choice<VisaType>> VisaTypes() =>
    [
        new(VisaType.B, "B – транзитная"),
        new(VisaType.C, "C – краткосрочная"),
        new(VisaType.D, "D – долгосрочная"),
    ];

    public static IEnumerable<Choice<Multiplicity>> Multiplicities() =>
    [
        new(Multiplicity.Single, "Однократная"),
        new(Multiplicity.Double, "Двукратная"),
        new(Multiplicity.Multiple, "Многократная"),
    ];

    public static IEnumerable<Choice<Sex>> Sexes() => [new(Sex.M, "Мужской"), new(Sex.F, "Женский")];

    public static IEnumerable<Choice<string>> Strings(IEnumerable<string> values) => values.Select(v => new Choice<string>(v, v));

    public static IEnumerable<Choice<string>> CountryList() => Countries.Codes.Select(c => new Choice<string>(c, Countries.Display(c)));

    public static IEnumerable<Choice<Role>> Roles() => Enum.GetValues<Role>().Select(r => new Choice<Role>(r, StatusNames.Of(r)));
}
