namespace VisaUchet.WinForms;

// Form Ф5 «Отчёт о заявках» (RPT-01 – RPT-03, SRS table 4.5)
internal sealed class ReportForm : OutputForm
{
    private const int DefaultPeriodDays = 30;
    private const string NoValue = "нет решений";

    private readonly DateTimePicker _from;
    private readonly DateTimePicker _to;
    private readonly ListView _indicators = new() { View = View.Details, Dock = DockStyle.Fill, FullRowSelect = true };
    private ApplicationReport? _report;

    public ReportForm(Services services)
        : base(services, "Отчёт о заявках")
    {
        var today = services.Clock.Today;
        _from = Ui.DatePicker(today.AddDays(-DefaultPeriodDays));
        _to = Ui.DatePicker(today);
        ClientSize = new Size(700, 560);
        BuildLayout();
    }

    public override void Render()
    {
        ApplicationReport? report = null;
        if (!Ui.Run(Services.Log, () => report = Services.Reports.Build(Ui.Date(_from), Ui.Date(_to))) || report is null)
        {
            return;
        }

        _report = report;
        _indicators.BeginUpdate();
        _indicators.Items.Clear();
        _indicators.Groups.Clear();
        foreach (var (section, items) in Sections(report))
        {
            var group = new ListViewGroup(section);
            _indicators.Groups.Add(group);
            foreach (var (caption, value) in items)
            {
                _indicators.Items.Add(new ListViewItem(new[] { caption, value }, group));
            }
        }

        _indicators.EndUpdate();
        MarkRendered();
    }

    protected override IReadOnlyList<string> GetPrintLines()
    {
        if (_report is null)
        {
            return [];
        }

        var lines = new List<string> { $"Период регистрации: {_report.From.ToString(Ui.DateFormat)} – {_report.To.ToString(Ui.DateFormat)}", string.Empty };
        lines.AddRange(Sections(_report).SelectMany(s => TextTable.Pairs(s.Section, s.Items)));
        return lines;
    }

    private void BuildLayout()
    {
        _indicators.Columns.Add("Показатель", 360);
        _indicators.Columns.Add("Значение", 260);
        var period = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6) };
        period.Controls.AddRange(new Control[]
        {
            new Label { Text = "Заявки, зарегистрированные с", AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, _from,
            new Label { Text = "по", AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, _to,
            Ui.Button("Сформировать", (_, _) => Render()),
        });
        Controls.Add(_indicators);
        Controls.Add(period);
        Controls.Add(Ui.Buttons(PdfButton(), Ui.Button("Закрыть", (_, _) => Close())));
    }

    private static List<(string Section, List<(string Caption, string Value)> Items)> Sections(ApplicationReport report)
    {
        var main = new List<(string, string)>
        {
            ("Зарегистрировано заявок", report.RegisteredCount().ToString()),
            ("Доля отказов, %", report.RefusalRate()?.ToString("0.0") ?? NoValue),
            ("Среднее время рассмотрения, раб. дней", report.AverageReviewDays()?.ToString("0.0") ?? NoValue),
            ("Решений позже контрольного срока", report.OverdueDecisions().ToString()),
        };
        var byStatus = report.CountByStatus().Select(p => (StatusNames.Of(p.Key), p.Value.ToString())).ToList();
        var byCategory = report.CountByCategory().Select(p => (p.Key, p.Value.ToString())).ToList();
        return
        [
            ("Основные показатели", main),
            ("Заявки по текущим статусам", byStatus),
            ("Заявки по категориям виз", byCategory),
        ];
    }
}
