namespace VisaUchet.WinForms;

// Form Ф1 «Регистрация заявки» (REG-01 – REG-06, SRS table 4.1)
internal sealed class RegistrationForm : Form
{
    private const int DefaultAgeYears = 30;
    private const int DefaultPassportYears = 5;
    private const int DefaultPeriodDays = 14;
    private const int DefaultStayDays = 14;

    private readonly Services _services;
    private readonly Employee _employee;
    private readonly FieldErrors _errors;
    private readonly TextBox _lastName = new() { Width = 220, CharacterCasing = CharacterCasing.Upper, MaxLength = DomainConstants.NameMaxLength };
    private readonly TextBox _firstName = new() { Width = 220, CharacterCasing = CharacterCasing.Upper, MaxLength = DomainConstants.NameMaxLength };
    private readonly DateTimePicker _birthDate;
    private readonly ComboBox _sex = Choices.Combo(Choices.Sexes(), 120);
    private readonly ComboBox _citizenship = Choices.Combo(Choices.CountryList(), 260);
    private readonly TextBox _passport = new() { Width = 120, CharacterCasing = CharacterCasing.Upper, MaxLength = DomainConstants.PassportMaxLength };
    private readonly DateTimePicker _passportExpiry;
    private readonly ComboBox _visaType = Choices.Combo(Choices.VisaTypes());
    private readonly ComboBox _category = Choices.Combo(Choices.Strings(DomainConstants.Categories), 260);
    private readonly ComboBox _multiplicity = Choices.Combo(Choices.Multiplicities());
    private readonly DateTimePicker _validFrom;
    private readonly DateTimePicker _validTo;
    private readonly NumericUpDown _stayDays = new() { Minimum = 1, Maximum = DomainConstants.LongTermMaxValidityDays, Value = DefaultStayDays, Width = 80 };
    private readonly CheckBox _urgent = new() { Text = "Срочное рассмотрение (2 рабочих дня)", AutoSize = true };

    public RegistrationForm(Services services, Employee employee)
    {
        _services = services;
        _employee = employee;
        var today = services.Clock.Today;
        _birthDate = Ui.DatePicker(today.AddYears(-DefaultAgeYears));
        _passportExpiry = Ui.DatePicker(today.AddYears(DefaultPassportYears));
        _validFrom = Ui.DatePicker(today);
        _validTo = Ui.DatePicker(today.AddDays(DefaultPeriodDays - 1));
        Choices.Select(_citizenship, "BLR");
        Choices.Select(_visaType, VisaType.C);
        _errors = new FieldErrors(this);
        Text = $"{Ui.Title} – Ф1 «Регистрация заявки»";
        Font = Ui.BaseFont;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 560);
        BuildLayout();
        MapErrors();
    }

    public string? RegisteredNumber { get; private set; }

    private void BuildLayout()
    {
        var grid = Ui.Grid(2);
        Ui.AddRow(grid, "Фамилия (латиницей) *", _lastName);
        Ui.AddRow(grid, "Имя (латиницей) *", _firstName);
        Ui.AddRow(grid, "Дата рождения *", _birthDate);
        Ui.AddRow(grid, "Пол *", _sex);
        Ui.AddRow(grid, "Гражданство *", _citizenship);
        Ui.AddRow(grid, "Номер паспорта *", _passport);
        Ui.AddRow(grid, "Паспорт действует до *", _passportExpiry);
        Ui.AddRow(grid, "Тип визы *", _visaType);
        Ui.AddRow(grid, "Категория *", _category);
        Ui.AddRow(grid, "Кратность *", _multiplicity);
        Ui.AddRow(grid, "Действует с *", _validFrom);
        Ui.AddRow(grid, "Действует по *", _validTo);
        Ui.AddRow(grid, "Срок пребывания, сут. *", _stayDays);
        Ui.AddRow(grid, string.Empty, _urgent);
        var register = Ui.Button("Зарегистрировать", (_, _) => TryRegister());

        // Full exit from the input loop: the operator can leave the form at any moment (also by Esc)
        var exit = Ui.Button("Выйти без регистрации (Esc)", (_, _) => Finish(DialogResult.Cancel));
        exit.ForeColor = Color.Firebrick;
        AcceptButton = register;
        CancelButton = exit;
        Controls.Add(grid);
        Controls.Add(Ui.Buttons(register, exit));
    }

    private void MapErrors() => _errors
        .Map(Fields.LastName, _lastName).Map(Fields.FirstName, _firstName).Map(Fields.BirthDate, _birthDate)
        .Map(Fields.Citizenship, _citizenship).Map(Fields.PassportNumber, _passport).Map(Fields.PassportExpiry, _passportExpiry)
        .Map(Fields.Category, _category).Map(Fields.Multiplicity, _multiplicity).Map(Fields.ValidFrom, _validFrom)
        .Map(Fields.ValidTo, _validTo).Map(Fields.StayDays, _stayDays);

    private ApplicationInput ReadInput() => new(
        _lastName.Text.Trim(),
        _firstName.Text.Trim(),
        Ui.Date(_birthDate),
        Choices.Selected<Sex>(_sex),
        Choices.Selected<string>(_citizenship),
        _passport.Text.Trim(),
        Ui.Date(_passportExpiry),
        Choices.Selected<VisaType>(_visaType),
        Choices.Selected<string>(_category),
        Choices.Selected<Multiplicity>(_multiplicity),
        Ui.Date(_validFrom),
        Ui.Date(_validTo),
        (int)_stayDays.Value,
        _urgent.Checked);

    // Activity «Регистрация заявки» (figure 2.1):
    // check data → [errors] show them at the fields and return to input (the operator fixes or leaves by «Выйти без регистрации»)
    //            → [no errors] check passport term → [insufficient] reject registration
    //                                              → [sufficient] find applicant → assign number, status and control date
    // Every completed branch ends in Finish – the single exit of the form
    private void TryRegister()
    {
        var input = ReadInput();
        var check = RegistrationValidator.ValidateData(input, _services.Clock.Today);
        if (!check.IsValid)
        {
            _errors.Show(check.Errors);
        }
        else if (!RegistrationValidator.IsPassportTermSufficient(input))
        {
            Reject(input);
        }
        else
        {
            Register(input);
        }
    }

    // REG-06: the registration is rejected with a message; the application is not created
    private void Reject(ApplicationInput input)
    {
        _errors.Show([new FieldError(Fields.PassportExpiry, "Срок действия паспорта недостаточен.")]);
        Ui.Error(
            $"Регистрация отклонена (REG-06).\nПаспорт должен действовать не менее {DomainConstants.PassportReserveDays} суток " +
            $"после окончания визы – до {RegistrationValidator.RequiredPassportExpiry(input).ToString(Ui.DateFormat)}.\n" +
            "Заявка не зарегистрирована.");
        Finish(DialogResult.Abort);
    }

    // REG-03: existing applicant is confirmed by the operator, otherwise a new one is created; REG-04: number and control date
    private void Register(ApplicationInput input)
    {
        ApplicantMatch? match = null;
        RegistrationResult? result = null;
        var ok = Ui.Run(_services.Log, () => match = _services.Applications.FindExistingApplicant(input), _errors)
            && (match is null || ConfirmExisting(match))
            && Ui.Run(_services.Log, () => result = _services.Applications.Register(input, _employee.Login, match?.Applicant.Id), _errors);
        if (ok && result is not null)
        {
            Ui.Info($"Заявка зарегистрирована.\nНомер: {result.Number}\nКонтрольный срок: {result.ControlDate.ToString(Ui.DateFormat)}");
            RegisteredNumber = result.Number;
            Finish(DialogResult.OK);
        }
    }

    // Single final node of the form: success, rejection and the operator's exit all close the form here
    private void Finish(DialogResult result)
    {
        DialogResult = result;
        Close();
    }

    private static bool ConfirmExisting(ApplicantMatch match) => Ui.Confirm(
        $"Найден заявитель {match.Applicant.FullName()}, {match.Applicant.BirthDate.ToString(Ui.DateFormat)}, " +
        $"{Countries.Display(match.Applicant.Citizenship)}.\nПрежних заявок: {match.PreviousApplications}.\n\n" +
        "Использовать данные этого заявителя? Паспортные данные будут обновлены.");
}
