namespace ChipEightEmulatorGUI.Models;

public sealed class Rom
{
    public Guid Guid { get; init; }
    public string Title { get; init; } = string.Empty;
    public long Size { get; init; }
    public TimeSpan PlayedFor { get; set; }
}