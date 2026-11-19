namespace VisaUchet.WinForms;

// Monospaced text table for printed output forms
internal static class TextTable
{
    private const string Gap = "  ";

    public static IEnumerable<string> Format(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var widths = headers.Select((h, i) => Math.Max(h.Length, rows.Count == 0 ? 0 : rows.Max(r => r[i].Length))).ToList();
        yield return Line(headers, widths);
        yield return string.Join(Gap, widths.Select(w => new string('-', w)));
        foreach (var row in rows)
        {
            yield return Line(row, widths);
        }
    }

    public static IEnumerable<string> Pairs(string section, IEnumerable<(string Caption, string Value)> items)
    {
        var list = items.ToList();
        var width = list.Count == 0 ? 0 : list.Max(i => i.Caption.Length);
        yield return section.ToUpperInvariant();
        foreach (var (caption, value) in list)
        {
            yield return $"  {caption.PadRight(width)}  {value}";
        }

        yield return string.Empty;
    }

    private static string Line(IReadOnlyList<string> cells, IReadOnlyList<int> widths) =>
        string.Join(Gap, cells.Select((c, i) => c.PadRight(widths[i]))).TrimEnd();
}
