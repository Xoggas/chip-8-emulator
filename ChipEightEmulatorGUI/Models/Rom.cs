namespace ChipEightEmulatorGUI.Models;

public sealed class Rom
{
    public Guid Guid { get; set; }
    public string Title { get; set; } = string.Empty;
    public long Size { get; set; }
}