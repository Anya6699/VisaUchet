namespace VisaUchet.WinForms;

// Login window (AUT-01); the message does not reveal whether the login exists
internal sealed class LoginForm : Form
{
    private readonly Services _services;
    private readonly TextBox _login = new() { Width = 220 };
    private readonly TextBox _password = new() { Width = 220, UseSystemPasswordChar = true };

    public LoginForm(Services services)
    {
        _services = services;
        Text = $"{Ui.Title} – вход";
        Font = Ui.BaseFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(380, 175);
        BuildLayout();
    }

    public Employee? Employee { get; private set; }

    private void BuildLayout()
    {
        var grid = Ui.Grid(2);
        grid.Controls.Add(new Label { Text = "Учёт заявок на получение визы", Font = Ui.HeaderFont, AutoSize = true }, 0, 0);
        grid.SetColumnSpan(grid.Controls[0], 2);
        Ui.AddRow(grid, "Логин", _login);
        Ui.AddRow(grid, "Пароль", _password);
        var enter = Ui.Button("Войти", (_, _) => TryLogin());
        var cancel = Ui.Button("Выход", (_, _) => Close());
        AcceptButton = enter;
        CancelButton = cancel;
        Controls.Add(grid);
        Controls.Add(Ui.Buttons(enter, cancel));
    }

    private void TryLogin()
    {
        Employee? employee = null;
        var ok = Ui.Run(_services.Log, () => employee = _services.Auth.Login(_login.Text, _password.Text));
        _password.Clear();
        if (!ok || employee is null)
        {
            _password.Focus();
            return;
        }

        Employee = employee;
        DialogResult = DialogResult.OK;
        Close();
    }
}
