namespace VisaUchet.WinForms;

// Main window: commands allowed for the role (AUT-01) and search of applications (CRD-01)
internal sealed class MainForm : Form
{
    private readonly Services _services;
    private readonly Employee _employee;
    private readonly IdleTracker _idle;
    private readonly TextBox _query = new() { Width = 260 };
    private readonly DataGridView _results = Ui.Table();

    public MainForm(Services services, Employee employee)
    {
        _services = services;
        _employee = employee;
        Text = $"{Ui.Title} – {employee.FullName} ({StatusNames.Of(employee.Role)})";
        Font = Ui.BaseFont;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(980, 560);
        MinimumSize = new Size(760, 420);
        BuildLayout();
        _idle = new IdleTracker(TimeSpan.FromMinutes(services.Settings.IdleLogoutMinutes), services.Clock);
        _idle.TimedOut += (_, _) => LogOut(byTimeout: true);
        FormClosed += (_, _) => _idle.Dispose();
        Shown += (_, _) => ShowAllApplications();
    }

    // true – the session ended by «Выход из аккаунта» or by idle timeout (SEC-02): the login window is shown again.
    // Closing the window by the cross closes the application
    public bool LoggedOut { get; private set; }

    private void BuildLayout()
    {
        Ui.Columns(_results, "Номер заявки", "Дата регистрации", "Заявитель", "Паспорт", "Статус", "Контрольный срок", "Просрочена");
        _results.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenSelected(); };
        _results.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; OpenSelected(); } };
        Controls.Add(_results);
        if (_employee.CanOpen(FormCode.ApplicationCard))
        {
            Controls.Add(BuildSearch());
        }

        Controls.Add(BuildCommands());
    }

    private FlowLayoutPanel BuildCommands()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6) };
        AddCommand(panel, FormCode.Registration, "Новая заявка (Ф1)", OpenRegistration);
        AddCommand(panel, FormCode.VisaJournal, "Журнал выданных виз (Ф4)", () => Show(new JournalForm(_services)));
        AddCommand(panel, FormCode.Report, "Отчёт о заявках (Ф5)", () => Show(new ReportForm(_services)));
        AddCommand(panel, FormCode.Users, "Пользователи", () => Show(new UsersForm(_services)));
        panel.Controls.Add(Ui.Button("Выход из аккаунта", (_, _) => LogOut(byTimeout: false)));
        return panel;
    }

    private void AddCommand(FlowLayoutPanel panel, FormCode form, string text, Action open)
    {
        if (_employee.CanOpen(form))
        {
            panel.Controls.Add(Ui.Button(text, (_, _) => open()));
        }
    }

    private FlowLayoutPanel BuildSearch()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6, 0, 6, 6) };
        var find = Ui.Button("Найти", (_, _) => Search());
        var all = Ui.Button("Показать все", (_, _) => ShowAllApplications());
        _query.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Search(); } };
        panel.Controls.Add(new Label { Text = "Номер заявки, фамилия (латиницей) или номер паспорта:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        panel.Controls.Add(_query);
        panel.Controls.Add(find);
        panel.Controls.Add(all);
        return panel;
    }

    // On opening and with an empty search field the list contains all applications
    private void ShowAllApplications()
    {
        if (_employee.CanOpen(FormCode.ApplicationCard))
        {
            _query.Clear();
            Search();
        }
    }

    private void Search()
    {
        IReadOnlyList<ApplicationSummary> found = [];
        if (Ui.Run(_services.Log, () => found = _services.Applications.Search(_query.Text)))
        {
            ShowResults(found);
        }
    }

    private void ShowResults(IReadOnlyList<ApplicationSummary> found)
    {
        _results.Rows.Clear();
        var today = _services.Clock.Today;
        foreach (var row in found)
        {
            var overdue = StatusRules.Default.IsPending(row.Status) && today > row.ControlDate;
            var index = _results.Rows.Add(row.Number, row.RegistrationDate.ToString("dd.MM.yyyy HH:mm"), row.ApplicantName,
                row.PassportNumber, StatusNames.Of(row.Status), row.ControlDate.ToString(Ui.DateFormat), overdue ? "Да" : string.Empty);
            if (overdue)
            {
                _results.Rows[index].DefaultCellStyle.ForeColor = Color.Firebrick;
            }
        }

        if (found.Count == 0 && _query.Text.Trim().Length > 0)
        {
            Ui.Info("Заявки не найдены.");
        }
    }

    private void OpenSelected()
    {
        if (_results.CurrentRow?.Cells[0].Value is string number)
        {
            OpenCard(number);
        }
    }

    private void OpenRegistration()
    {
        using var form = new RegistrationForm(_services, _employee);
        if (form.ShowDialog(this) == DialogResult.OK && form.RegisteredNumber is { } number)
        {
            OpenCard(number);
        }
    }

    private void OpenCard(string number)
    {
        using var card = new ApplicationCardForm(_services, _employee, number);
        card.ShowDialog(this);
        Search();
    }

    private void Show(Form form)
    {
        using (form)
        {
            form.ShowDialog(this);
        }
    }

    // Closes every open window of the session and returns to the login window
    private void LogOut(bool byTimeout)
    {
        if (!byTimeout)
        {
            _services.Log.Info($"Выход из аккаунта: {_employee.Login}");
        }

        LoggedOut = true;
        foreach (var form in System.Windows.Forms.Application.OpenForms.Cast<Form>().Where(f => f != this).ToList())
        {
            form.Close();
        }

        Close();
    }
}
