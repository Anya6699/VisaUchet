namespace VisaUchet.WinForms;

// User administration (AUT-02): create accounts, block and assign roles
internal sealed class UsersForm : Form
{
    private readonly Services _services;
    private readonly DataGridView _grid = Ui.Table();
    private readonly TextBox _login = new() { Width = 140 };
    private readonly TextBox _fullName = new() { Width = 240 };
    private readonly ComboBox _role = Choices.Combo(Choices.Roles(), 220);
    private readonly TextBox _password = new() { Width = 140, UseSystemPasswordChar = true };

    public UsersForm(Services services)
    {
        _services = services;
        Text = $"{Ui.Title} – пользователи";
        Font = Ui.BaseFont;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 560);
        BuildLayout();
        Load += (_, _) => Reload();
    }

    private void BuildLayout()
    {
        Ui.Columns(_grid, "Логин", "ФИО", "Роль", "Заблокирован", "Блокировка входа до");
        var create = new GroupBox { Text = "Новая учётная запись", Dock = DockStyle.Bottom, Height = 150 };
        var form = Ui.Grid(4);
        Ui.AddRow(form, "Логин", _login);
        Ui.AddRow(form, "ФИО", _fullName);
        Ui.AddRow(form, "Роль", _role);
        Ui.AddRow(form, "Пароль", _password);
        form.Controls.Add(Ui.Button("Создать", (_, _) => Create()));
        create.Controls.Add(form);
        Controls.Add(_grid);
        Controls.Add(create);
        Controls.Add(Ui.Buttons(
            Ui.Button("Заблокировать", (_, _) => ForSelected(login => _services.Users.SetBlocked(login, true))),
            Ui.Button("Разблокировать", (_, _) => ForSelected(login => _services.Users.SetBlocked(login, false))),
            Ui.Button("Назначить выбранную роль", (_, _) => ForSelected(login => _services.Users.SetRole(login, Choices.Selected<Role>(_role)))),
            Ui.Button("Закрыть", (_, _) => Close())));
    }

    private void Reload()
    {
        IReadOnlyList<Employee> employees = [];
        if (!Ui.Run(_services.Log, () => employees = _services.Users.List()))
        {
            return;
        }

        _grid.Rows.Clear();
        foreach (var e in employees)
        {
            _grid.Rows.Add(e.Login, e.FullName, StatusNames.Of(e.Role), e.IsBlocked ? "Да" : "Нет",
                e.LockedUntil?.ToString("dd.MM.yyyy HH:mm") ?? string.Empty);
        }
    }

    private void Create()
    {
        var created = Ui.Run(_services.Log, () =>
            _services.Users.Create(_login.Text, _fullName.Text, Choices.Selected<Role>(_role), _password.Text));
        _password.Clear();
        if (created)
        {
            _login.Clear();
            _fullName.Clear();
            Reload();
        }
    }

    private void ForSelected(Action<string> action)
    {
        if (_grid.CurrentRow?.Cells[0].Value is not string login)
        {
            Ui.Error("Выберите сотрудника в списке.");
            return;
        }

        if (Ui.Run(_services.Log, () => action(login)))
        {
            Reload();
        }
    }
}
