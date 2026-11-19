namespace VisaUchet.WinForms;

// Message boxes and small layout helpers shared by all forms
internal static class Ui
{
    public const string Title = "ИС «ВизаУчет»";
    public const string DateFormat = "dd.MM.yyyy";
    public static readonly Font BaseFont = new("Segoe UI", 9F);
    public static readonly Font HeaderFont = new("Segoe UI", 11F, FontStyle.Bold);

    public static void Error(string message) =>
        MessageBox.Show(message, Title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static void Info(string message) =>
        MessageBox.Show(message, Title, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static bool Confirm(string message) =>
        MessageBox.Show(message, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    // Logs the technical details and shows a short message without them
    public static void Fail(IAppLog log, string message, Exception exception)
    {
        log.Error(message, exception);
        Error($"{message}.\nПодробности записаны в журнал работы приложения.");
    }

    // Runs a UI action: business errors are shown to the user, database errors are logged
    public static bool Run(IAppLog log, Action action, FieldErrors? errors = null)
    {
        try
        {
            errors?.Clear();
            action();
            return true;
        }
        catch (DomainException ex)
        {
            if (errors is null)
            {
                Error(ex.Message);
            }
            else
            {
                errors.Show(ex.Errors);
            }

            return false;
        }
        catch (System.Data.Common.DbException ex)
        {
            Fail(log, "Ошибка базы данных", ex);
            return false;
        }
    }

    public static TableLayoutPanel Grid(int columns)
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = columns,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Padding = new Padding(8),
        };
        for (var i = 0; i < columns; i++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        return panel;
    }

    public static void AddRow(TableLayoutPanel panel, string caption, Control control)
    {
        var label = new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 12, 3) };
        panel.Controls.Add(label);
        panel.Controls.Add(control);
    }

    public static Button Button(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, AutoSize = true, Padding = new Padding(8, 2, 8, 2) };
        button.Click += onClick;
        return button;
    }

    public static DateTimePicker DatePicker(DateOnly value) => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = DateFormat,
        Width = 120,
        Value = value.ToDateTime(TimeOnly.MinValue),
    };

    public static DateOnly Date(DateTimePicker picker) => DateOnly.FromDateTime(picker.Value);

    public static DataGridView Table() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
        BackgroundColor = SystemColors.Window,
    };

    public static void Columns(DataGridView grid, params string[] headers)
    {
        grid.Columns.Clear();
        foreach (var header in headers)
        {
            grid.Columns.Add(header, header);
        }
    }

    public static FlowLayoutPanel Buttons(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(6) };
        panel.Controls.AddRange(controls);
        return panel;
    }
}
