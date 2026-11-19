namespace VisaUchet.WinForms;

// Form Ф2 «Смена статуса» (STS-01 – STS-05, SRS table 4.2)
internal sealed class StatusChangeForm : Form
{
    private readonly Services _services;
    private readonly Employee _employee;
    private readonly string _number;
    private readonly FieldErrors _errors;
    private readonly ComboBox _status = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly TextBox _comment = new() { Multiline = true, Width = 360, Height = 70, MaxLength = DomainConstants.CommentMaxLength, ScrollBars = ScrollBars.Vertical };
    private readonly GroupBox _visaGroup = new() { Text = "Сведения о визе (только для статуса «Виза выдана»)", Dock = DockStyle.Top, AutoSize = true };
    private readonly TextBox _visaNumber = new() { Width = 120, CharacterCasing = CharacterCasing.Upper, MaxLength = 10, Text = DomainConstants.VisaNumberPrefix };
    private readonly DateTimePicker _validFrom;
    private readonly DateTimePicker _validTo;
    private readonly ComboBox _multiplicity = Choices.Combo(Choices.Multiplicities());
    private readonly NumericUpDown _stayDays = new() { Minimum = 1, Maximum = DomainConstants.LongTermMaxValidityDays, Width = 80 };
    private readonly NumericUpDown _fee = new() { Minimum = 0, Maximum = DomainConstants.FeeMax, DecimalPlaces = 2, Width = 100 };
    private readonly ComboBox _currency = Choices.Combo(Choices.Strings(DomainConstants.Currencies), 80);
    private ApplicationCard? _card;

    public StatusChangeForm(Services services, Employee employee, string number)
    {
        _services = services;
        _employee = employee;
        _number = number;
        _validFrom = Ui.DatePicker(services.Clock.Today);
        _validTo = Ui.DatePicker(services.Clock.Today);
        _errors = new FieldErrors(this);
        Text = $"{Ui.Title} – Ф2 «Смена статуса»";
        Font = Ui.BaseFont;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 520);
        Load += (_, _) => LoadCard();
    }

    private void LoadCard()
    {
        if (Ui.Run(_services.Log, () => _card = _services.Applications.GetCard(_number)) && _card is not null)
        {
            BuildLayout(_card);
            FillVisaDefaults(_card.Application.Requested);
            _status.Items.AddRange(_card.AllowedStatuses.Select(s => (object)new Choice<ApplicationStatus>(s, StatusNames.Of(s))).ToArray());
            _status.SelectedIndexChanged += (_, _) => UpdateVisaGroup();
            if (_status.Items.Count > 0)
            {
                _status.SelectedIndex = 0;
            }

            UpdateVisaGroup();
        }
        else
        {
            Finish(DialogResult.Cancel);
        }
    }

    private void BuildLayout(ApplicationCard card)
    {
        var main = Ui.Grid(2);
        Ui.AddRow(main, "Номер заявки", new Label { Text = card.Application.Number, AutoSize = true });
        Ui.AddRow(main, "Текущий статус", new Label { Text = StatusNames.Of(card.Application.Status), AutoSize = true });
        Ui.AddRow(main, "Новый статус *", _status);
        Ui.AddRow(main, "Комментарий", _comment);
        var visa = Ui.Grid(2);
        Ui.AddRow(visa, "Номер бланка *", _visaNumber);
        Ui.AddRow(visa, "Действует с *", _validFrom);
        Ui.AddRow(visa, "Действует по *", _validTo);
        Ui.AddRow(visa, "Кратность *", _multiplicity);
        Ui.AddRow(visa, "Срок пребывания, сут. *", _stayDays);
        Ui.AddRow(visa, "Консульский сбор *", _fee);
        Ui.AddRow(visa, "Валюта *", _currency);
        _visaGroup.Controls.Add(visa);
        var save = Ui.Button("Сохранить", (_, _) => Save());
        var cancel = Ui.Button("Отмена (Esc)", (_, _) => Finish(DialogResult.Cancel));
        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(_visaGroup);
        Controls.Add(main);
        Controls.Add(Ui.Buttons(save, cancel));
        _errors.Map(CommentRules.Field, _comment).Map(Fields.VisaNumber, _visaNumber).Map(Fields.ValidFrom, _validFrom)
            .Map(Fields.ValidTo, _validTo).Map(Fields.Multiplicity, _multiplicity).Map(Fields.StayDays, _stayDays)
            .Map(Fields.Fee, _fee).Map(Fields.Currency, _currency);
    }

    // The issued visa is pre-filled with the requested parameters; they can only be reduced
    private void FillVisaDefaults(VisaParameters requested)
    {
        _validFrom.Value = requested.ValidFrom.ToDateTime(TimeOnly.MinValue);
        _validTo.Value = requested.ValidTo.ToDateTime(TimeOnly.MinValue);
        Choices.Select(_multiplicity, requested.Multiplicity);
        _stayDays.Value = requested.StayDays;
    }

    private bool IssuingVisa => _status.SelectedItem is Choice<ApplicationStatus> { Value: ApplicationStatus.Issued };

    private void UpdateVisaGroup() => _visaGroup.Enabled = IssuingVisa;

    // Activity «Изменение статуса заявки» (figure 2.2): check data → [wrong] show error, nothing is saved
    // → [correct] history record + status (+ visa journal) in one transaction → card refresh by the caller.
    // Both branches merge at the end of the method – a single exit
    private void Save()
    {
        if (_status.SelectedItem is Choice<ApplicationStatus> choice)
        {
            var request = new StatusChangeRequest(_number, choice.Value, _comment.Text, IssuingVisa ? ReadVisa() : null);
            if (Ui.Run(_services.Log, () => _services.Applications.ChangeStatus(request, _employee.Login), _errors))
            {
                Finish(DialogResult.OK);
            }
        }
        else
        {
            Ui.Error("Выберите новый статус. Данные не сохранены.");
        }
    }

    // Single final node of the form
    private void Finish(DialogResult result)
    {
        DialogResult = result;
        Close();
    }

    private VisaIssueInput ReadVisa() => new(
        _visaNumber.Text.Trim(),
        Ui.Date(_validFrom),
        Ui.Date(_validTo),
        Choices.Selected<Multiplicity>(_multiplicity),
        (int)_stayDays.Value,
        _fee.Value,
        Choices.Selected<string>(_currency));
}
