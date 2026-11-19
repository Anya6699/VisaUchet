using VisaUchet.Infrastructure;
using WinFormsApp = System.Windows.Forms.Application;

namespace VisaUchet.WinForms;

internal static class Program
{
    private const string SettingsFile = "appsettings.json";

    [STAThread]
    private static void Main()
    {
        WinFormsApp.EnableVisualStyles();
        WinFormsApp.SetCompatibleTextRenderingDefault(false);
        var services = TryStart();
        if (services is null)
        {
            return;
        }

        WinFormsApp.ThreadException += (_, e) => Ui.Fail(services.Log, "Непредвиденная ошибка", e.Exception);
        RunSessions(services);
    }

    private static Services? TryStart()
    {
        AppSettings settings;
        try
        {
            settings = AppSettings.Load(Path.Combine(AppContext.BaseDirectory, SettingsFile));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            Ui.Error($"Ошибка настроек: {ex.Message}");
            return null;
        }

        var log = new FileAppLog(Path.Combine(AppContext.BaseDirectory, settings.LogFile));
        try
        {
            var services = Services.Create(settings, log);
            services.Retention.Apply();
            return services;
        }
        catch (Exception ex) when (ex is System.Data.Common.DbException or InvalidOperationException or ArgumentException)
        {
            Ui.Fail(log, "Не удалось подключиться к базе данных. Проверьте, что SQL Server запущен и база VisaUchet создана", ex);
            return null;
        }
    }

    // After an idle logout (SEC-02) the login window is shown again
    private static void RunSessions(Services services)
    {
        while (true)
        {
            using var login = new LoginForm(services);
            if (login.ShowDialog() != DialogResult.OK || login.Employee is null)
            {
                return;
            }

            using var main = new MainForm(services, login.Employee);
            WinFormsApp.Run(main);
            if (!main.LoggedOutByTimeout)
            {
                return;
            }
        }
    }
}
