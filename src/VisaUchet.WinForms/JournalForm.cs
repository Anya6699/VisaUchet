namespace VisaUchet.WinForms;

// Form Ф4 «Журнал выданных виз» (JRN-02 – JRN-04, SRS table 4.4)
internal sealed class JournalForm : OutputForm
{
    private static readonly string[] Headers =
    [
        "№", "Номер визы", "Дата выдачи", "Номер заявки", "Фамилия и имя", "Гражданство", "Тип", "Категория",
        "Кратность", "Действует с – по", "Пребывание", "Сбор", "Валюта", "Состояние", "Сотрудник",
    ];

    private readonly DateTimePicker _from;
    private readonly DateTimePicker _to;
    private readonly ComboBox _type = Choices.Combo(new[] { new Choice<VisaType?>(null, "Все") }
        .Concat(Choices.VisaTypes().Select(c => new Choice<VisaType?>(c.Value, c.Text))), 160);
    private readonly ComboBox _state = Choices.Combo(new[]
    {
        new Choice<VisaState?>(null, "Все"),
        new Choice<VisaState?>(VisaState.Valid, StatusNames.Of(VisaState.Valid)),
        new Choice<VisaState?>(VisaState.Cancelled, StatusNames.Of(VisaState.Cancelled)),
    }, 140);
    private readonly DataGridView _grid = Ui.Table();
    private readonly Label _totals = new() { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(8, 4, 8, 4) };
    private VisaJournal? _journal;

    public JournalForm(Services services)
        : base(services, "Журнал выданных виз")
    {
        var filter = VisaJournalService.DefaultFilter(services.Clock.Today);
        _from = Ui.DatePicker(filter.From);
        _to = Ui.DatePicker(filter.To);
        ClientSize = new Size(1200, 600);
        BuildLayout();
        Load += (_, _) => Render();
    }

    public override void Render()
    {
        var filter = new JournalFilter(Ui.Date(_from), Ui.Date(_to), Choices.Selected<VisaType?>(_type), Choices.Selected<VisaState?>(_state));
        VisaJournal? journal = null;
        if (!Ui.Run(Services.Log, () => journal = Services.Journal.Query(filter)) || journal is null)
        {
            return;
        }

        _journal = journal;
        _grid.Rows.Clear();
        foreach (var row in Rows(journal))
        {
            _grid.Rows.Add(row.Cast<object>().ToArray());
        }

        _totals.Text = TotalsText(journal);
        MarkRendered();
    }

    protected override IReadOnlyList<string> GetPrintLines()
    {
        if (_journal is null)
        {
            return [];
        }

        var lines = new List<string> { $"Период выдачи: {_journal.Filter.From.ToString(Ui.DateFormat)} – {_journal.Filter.To.ToString(Ui.DateFormat)}", string.Empty };
        lines.AddRange(TextTable.Format(Headers, Rows(_journal)));
        lines.Add(string.Empty);
        lines.AddRange(TotalsText(_journal).Split('\n'));
        return lines;
    }

    private void BuildLayout()
    {
        Ui.Columns(_grid, Headers);
        var filter = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6) };
        filter.Controls.AddRange(new Control[]
        {
            Caption("Выданы с"), _from, Caption("по"), _to, Caption("Тип визы"), _type, Caption("Состояние"), _state,
            Ui.Button("Показать", (_, _) => Render()),
        });
        Controls.Add(_grid);
        Controls.Add(filter);
        Controls.Add(_totals);
        Controls.Add(Ui.Buttons(PdfButton(), Ui.Button("Закрыть", (_, _) => Close())));
    }

    private static Label Caption(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(6, 8, 3, 3) };

    private static List<IReadOnlyList<string>> Rows(VisaJournal journal) => journal.Rows
        .Select((v, i) => (IReadOnlyList<string>)new List<string>
        {
            (i + 1).ToString(), v.VisaNumber, v.IssueDate.ToString(Ui.DateFormat), v.ApplicationNumber ?? "удалена",
            v.HolderName, v.HolderCitizenship, v.Parameters.Type.ToString(), v.Parameters.Category,
            StatusNames.Of(v.Parameters.Multiplicity),
            $"{v.Parameters.ValidFrom.ToString(Ui.DateFormat)} – {v.Parameters.ValidTo.ToString(Ui.DateFormat)}",
            v.Parameters.StayDays.ToString("000"), v.Fee.ToString("0.00"), v.Currency, StatusNames.Of(v.State), v.IssuedBy,
        })
        .ToList();

    private static string TotalsText(VisaJournal journal)
    {
        var fees = journal.Currencies().Select(c => $"{c} {journal.TotalFee(c):0.00}").ToList();
        return $"Выдано виз: {journal.CountVisas()}, в том числе аннулировано: {journal.CountCancelled()}\n" +
               $"Сумма сборов: {(fees.Count == 0 ? "0" : string.Join("; ", fees))}";
    }
}
