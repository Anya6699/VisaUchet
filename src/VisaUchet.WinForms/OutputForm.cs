using System.Drawing.Printing;

namespace VisaUchet.WinForms;

// Contract of saving an output form to PDF (CRD-04, JRN-04, RPT-03)
public interface IExportable
{
    void ExportPdf(string path);
}

// Base class of output forms Ф3–Ф5: printable text content saved through «Microsoft Print to PDF»
internal abstract class OutputForm : Form, IExportable
{
    public const string PdfPrinter = "Microsoft Print to PDF";
    private const float PageMargin = 50F;
    private static readonly Font PrintFont = new("Consolas", 8.5F);
    private static readonly Font PrintTitleFont = new("Segoe UI", 12F, FontStyle.Bold);

    protected OutputForm(Services services, string title)
    {
        Services = services;
        ReportTitle = title;
        Text = $"{Ui.Title} – {title}";
        Font = Ui.BaseFont;
        StartPosition = FormStartPosition.CenterParent;
        CreatedAt = services.Clock.Now;
    }

    protected Services Services { get; }

    protected string ReportTitle { get; }

    protected DateTime CreatedAt { get; private set; }

    // Fills the controls of the form with current data
    public abstract void Render();

    public void ExportPdf(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var lines = PrintLines();
        var position = 0;
        using var document = new PrintDocument { DocumentName = ReportTitle };
        document.PrinterSettings.PrinterName = PdfPrinter;
        if (!document.PrinterSettings.IsValid)
        {
            throw new InvalidOperationException($"В системе не найден принтер «{PdfPrinter}».");
        }

        document.PrinterSettings.PrintToFile = true;
        document.PrinterSettings.PrintFileName = path;
        document.PrintPage += (_, e) => position = PrintPage(e, lines, position);
        document.Print();
    }

    // Text lines of the printed document; the first line is the title
    protected abstract IReadOnlyList<string> GetPrintLines();

    protected void MarkRendered() => CreatedAt = Services.Clock.Now;

    protected Button PdfButton() => Ui.Button("Сохранить в PDF", (_, _) => SavePdf());

    private IReadOnlyList<string> PrintLines()
    {
        var lines = new List<string> { ReportTitle, $"Сформировано: {CreatedAt:dd.MM.yyyy HH:mm}", string.Empty };
        lines.AddRange(GetPrintLines());
        return lines;
    }

    private void SavePdf()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"{ReportTitle}_{Services.Clock.Now:yyyyMMdd_HHmm}.pdf",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            ExportPdf(dialog.FileName);
            Ui.Info($"Файл сохранён: {dialog.FileName}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidPrinterException or System.ComponentModel.Win32Exception)
        {
            Ui.Fail(Services.Log, "Не удалось сохранить PDF", ex);
        }
    }

    // Prints lines from 'start' while they fit; returns the index of the next line
    private static int PrintPage(PrintPageEventArgs e, IReadOnlyList<string> lines, int start)
    {
        var graphics = e.Graphics ?? throw new InvalidOperationException("Нет поверхности печати.");
        var y = PageMargin;
        var bottom = e.PageBounds.Height - PageMargin;
        var index = start;
        while (index < lines.Count)
        {
            var font = index == 0 ? PrintTitleFont : PrintFont;
            var height = font.GetHeight(graphics);
            if (y + height > bottom)
            {
                break;
            }

            graphics.DrawString(lines[index], font, Brushes.Black, PageMargin, y);
            y += height;
            index++;
        }

        e.HasMorePages = index < lines.Count;
        return index;
    }
}
