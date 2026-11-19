namespace VisaUchet.WinForms;

// SEC-02: raises TimedOut after the configured period without keyboard or mouse input
internal sealed class IdleTracker : IMessageFilter, IDisposable
{
    private const int WmKeyDown = 0x0100;
    private const int WmMouseMove = 0x0200;
    private const int WmLeftButtonDown = 0x0201;
    private const int WmRightButtonDown = 0x0204;
    private const int WmMouseWheel = 0x020A;
    private const int CheckIntervalMs = 30_000;

    private readonly TimeSpan _limit;
    private readonly IClock _clock;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = CheckIntervalMs };
    private DateTime _lastActivity;

    public IdleTracker(TimeSpan limit, IClock clock)
    {
        _limit = limit;
        _clock = clock;
        _lastActivity = clock.Now;
        _timer.Tick += (_, _) => Check();
        System.Windows.Forms.Application.AddMessageFilter(this);
        _timer.Start();
    }

    public event EventHandler? TimedOut;

    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg is WmKeyDown or WmMouseMove or WmLeftButtonDown or WmRightButtonDown or WmMouseWheel)
        {
            _lastActivity = _clock.Now;
        }

        return false;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        System.Windows.Forms.Application.RemoveMessageFilter(this);
    }

    private void Check()
    {
        if (_clock.Now - _lastActivity >= _limit)
        {
            _timer.Stop();
            TimedOut?.Invoke(this, EventArgs.Empty);
        }
    }
}
