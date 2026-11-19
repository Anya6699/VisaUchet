namespace VisaUchet.WinForms;

// Form Ф3 «Карточка заявки» (CRD-02 – CRD-04, SRS table 4.3)
internal sealed class ApplicationCardForm : OutputForm
{
    private static readonly string[] HistoryHeaders = ["Дата и время", "Прежний статус", "Новый статус", "Сотрудник", "Комментарий"];

    private readonly Employee _employee;
    private readonly string _number;
    private readonly Label _overdue = new() { Text = "Просрочена", ForeColor = Color.Firebrick, Font = Ui.HeaderFont, AutoSize = true, Visible = false };
    private readonly ListView _info = new() { View = View.Details, Dock = DockStyle.Top, Height = 330, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.None };
    private readonly DataGridView _history = Ui.Table();
    private readonly Button _changeStatus;
    private List<(string Section, List<(string Caption, string Value)> Items)> _sections = [];

    public ApplicationCardForm(Services services, Employee employee, string number)
        : base(services, $"Карточка заявки {number}")
    {
        _employee = employee;
        _number = number;
        ClientSize = new Size(900, 640);
        _changeStatus = Ui.Button("Сменить статус (Ф2)", (_, _) => ChangeStatus());
        BuildLayout();
        Load += (_, _) => Render();
    }

    public override void Render()
    {
        ApplicationCard? card = null;
        if (!Ui.Run(Services.Log, () => card = Services.Applications.GetCard(_number)) || card is null)
        {
            Close();
            return;
        }

        _sections = CardSections.Build(card);
        FillInfo();
        FillHistory(card.Application);
        _overdue.Visible = card.IsOverdue;
        _changeStatus.Visible = _employee.CanOpen(FormCode.StatusChange) && card.AllowedStatuses.Count > 0;
        MarkRendered();
    }

    protected override IReadOnlyList<string> GetPrintLines()
    {
        var lines = _sections.SelectMany(s => TextTable.Pairs(s.Section, s.Items)).ToList();
        lines.Add("ИСТОРИЯ СТАТУСОВ");
        lines.AddRange(TextTable.Format(HistoryHeaders, GridRows()));
        return lines;
    }

    private void BuildLayout()
    {
        _info.Columns.Add("Поле", 230);
        _info.Columns.Add("Значение", 600);
        Ui.Columns(_history, HistoryHeaders);
        var header = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6) };
        header.Controls.Add(new Label { Text = $"Заявка {_number}", Font = Ui.HeaderFont, AutoSize = true });
        header.Controls.Add(_overdue);
        var historyCaption = new Label { Text = "История статусов", Dock = DockStyle.Top, Font = Ui.HeaderFont, Height = 28 };
        Controls.Add(_history);
        Controls.Add(historyCaption);
        Controls.Add(_info);
        Controls.Add(header);
        Controls.Add(Ui.Buttons(_changeStatus, PdfButton(), Ui.Button("Закрыть", (_, _) => Close())));
    }

    private void FillInfo()
    {
        _info.BeginUpdate();
        _info.Items.Clear();
        _info.Groups.Clear();
        foreach (var (section, items) in _sections)
        {
            var group = new ListViewGroup(section);
            _info.Groups.Add(group);
            foreach (var (caption, value) in items)
            {
                _info.Items.Add(new ListViewItem(new[] { caption, value }, group));
            }
        }

        _info.EndUpdate();
    }

    private void FillHistory(VisaApplication application)
    {
        _history.Rows.Clear();
        foreach (var record in application.History)
        {
            _history.Rows.Add(record.ChangedAt.ToString("dd.MM.yyyy HH:mm:ss"),
                record.OldStatus is { } old ? StatusNames.Of(old) : "–",
                StatusNames.Of(record.NewStatus), record.EmployeeLogin, record.Comment ?? string.Empty);
        }
    }

    private List<IReadOnlyList<string>> GridRows() =>
        _history.Rows.Cast<DataGridViewRow>()
            .Select(r => (IReadOnlyList<string>)r.Cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? string.Empty).ToList())
            .ToList();

    private void ChangeStatus()
    {
        using var form = new StatusChangeForm(Services, _employee, _number);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            Render();
        }
    }
}

// Sections and values of the card (shared by the screen and the printed copy)
internal static class CardSections
{
    public static List<(string Section, List<(string Caption, string Value)> Items)> Build(ApplicationCard card)
    {
        var a = card.Application;
        var sections = new List<(string, List<(string, string)>)>
        {
            ("Заявка", ApplicationItems(card)),
            ("Заявитель", ApplicantItems(a.Applicant, card.PreviousApplications)),
            ("Запрошенная виза", VisaItems(a.Requested)),
        };
        if (card.Visa is { } visa)
        {
            sections.Add(("Выданная виза", IssuedVisaItems(visa)));
        }

        return sections;
    }

    private static List<(string, string)> ApplicationItems(ApplicationCard card)
    {
        var a = card.Application;
        var since = a.History.Count > 0 ? a.History[^1].ChangedAt.ToString("dd.MM.yyyy HH:mm") : string.Empty;
        return
        [
            ("Номер", a.Number),
            ("Дата регистрации", a.RegistrationDate.ToString("dd.MM.yyyy HH:mm")),
            ("Срочность", a.IsUrgent ? "Срочная" : "Обычная"),
            ("Контрольный срок", a.ControlDate.ToString(Ui.DateFormat) + (card.IsOverdue ? " (просрочена)" : string.Empty)),
            ("Текущий статус", $"{StatusNames.Of(a.Status)} с {since}"),
        ];
    }

    private static List<(string, string)> ApplicantItems(Applicant p, int previous) =>
    [
        ("Фамилия и имя", p.FullName()),
        ("Дата рождения", p.BirthDate.ToString(Ui.DateFormat)),
        ("Пол", p.Sex == Sex.M ? "Мужской" : "Женский"),
        ("Гражданство", Countries.Display(p.Citizenship)),
        ("Паспорт", $"{p.PassportNumber}, действует до {p.PassportExpiry.ToString(Ui.DateFormat)}"),
        ("Прежних заявок", previous.ToString()),
    ];

    private static List<(string, string)> VisaItems(VisaParameters v) =>
    [
        ("Тип, категория", $"{v.Type}, {v.Category}"),
        ("Кратность", StatusNames.Of(v.Multiplicity)),
        ("Период", $"{v.ValidFrom.ToString(Ui.DateFormat)} – {v.ValidTo.ToString(Ui.DateFormat)} ({v.PeriodDays} сут.)"),
        ("Срок пребывания", $"{v.StayDays:000} сут."),
    ];

    private static List<(string, string)> IssuedVisaItems(IssuedVisa visa)
    {
        var items = new List<(string, string)>
        {
            ("Номер визы", visa.VisaNumber),
            ("Дата выдачи", visa.IssueDate.ToString(Ui.DateFormat)),
        };
        items.AddRange(VisaItems(visa.Parameters));
        items.Add(("Консульский сбор", $"{visa.Currency} {visa.Fee:0.00}"));
        items.Add(("Состояние", StatusNames.Of(visa.State)
            + (visa.CancelDate is { } date ? $" с {date.ToString(Ui.DateFormat)}: {visa.CancelReason}" : string.Empty)));
        return items;
    }
}
