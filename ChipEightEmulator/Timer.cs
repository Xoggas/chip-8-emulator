namespace ChipEightEmulator;

public class Timer
{
    private readonly int _interval;
    private readonly bool _autoReset;

    public Timer(int interval, bool autoReset = false)
    {
        _interval = interval;
        _autoReset = autoReset;
        Ticks = interval;
    }

    public int Ticks { get; set; }

    public void Update()
    {
        if (Ticks > 0)
        {
            Ticks--;
        }
        else if (_autoReset)
        {
            Ticks = _interval;
        }
    }
}