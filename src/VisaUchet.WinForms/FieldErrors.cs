namespace VisaUchet.WinForms;

// UI-03: shows validation errors next to the corresponding input fields
internal sealed class FieldErrors
{
    private readonly ErrorProvider _provider;
    private readonly Dictionary<string, Control> _controls = new();

    public FieldErrors(Form owner)
    {
        _provider = new ErrorProvider(owner.Container ?? new System.ComponentModel.Container())
        {
            BlinkStyle = ErrorBlinkStyle.NeverBlink,
        };
    }

    public FieldErrors Map(string field, Control control)
    {
        _controls[field] = control;
        return this;
    }

    public void Clear()
    {
        foreach (var control in _controls.Values)
        {
            _provider.SetError(control, string.Empty);
        }
    }

    // Errors without a mapped field are shown in a message box
    public void Show(IReadOnlyList<FieldError> errors)
    {
        Clear();
        var unmapped = new List<string>();
        foreach (var error in errors)
        {
            if (_controls.TryGetValue(error.Field, out var control))
            {
                var current = _provider.GetError(control);
                _provider.SetError(control, string.IsNullOrEmpty(current) ? error.Message : $"{current}\n{error.Message}");
            }
            else
            {
                unmapped.Add(error.Message);
            }
        }

        if (unmapped.Count > 0)
        {
            Ui.Error(string.Join("\n", unmapped));
        }

        _controls.Values.FirstOrDefault(c => !string.IsNullOrEmpty(_provider.GetError(c)))?.Focus();
    }
}
