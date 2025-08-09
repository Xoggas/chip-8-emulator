using System.Diagnostics;
using ChipEightEmulatorGUI.Models;

namespace ChipEightEmulatorGUI.Services;

public sealed class EmulatorService
{
    private const string EmulatorExeName = "ChipEightEmulator.exe";

    private readonly Dictionary<Rom, EmulatorInstance> _emulatorInstances = [];
    private readonly RomStorageService _romStorageService;

    public EmulatorService(RomStorageService romStorageService)
    {
        _romStorageService = romStorageService;
    }

    public IReadOnlyDictionary<Rom, EmulatorInstance> EmulatorInstances => _emulatorInstances;

    public EmulatorInstance? CreateEmulatorInstance(Rom rom)
    {
        var path = _romStorageService.GetRomPath(rom);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = EmulatorExeName,
            Arguments = path,
            CreateNoWindow = true
        };

        var process = Process.Start(processStartInfo);

        if (process is null)
        {
            return null;
        }

        var emulatorInstance = new EmulatorInstance(rom, process, DateTime.Now);

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => OnEmulatorInstanceClosed(emulatorInstance);

        _emulatorInstances.Add(rom, emulatorInstance);

        return emulatorInstance;
    }

    public void StopEmulatorInstanceForRom(Rom rom)
    {
        var emulatorInstance = _emulatorInstances[rom];
        emulatorInstance.Process.Kill();
    }

    private void OnEmulatorInstanceClosed(EmulatorInstance emulatorInstance)
    {
        _emulatorInstances.Remove(emulatorInstance.Rom);
    }
}

public sealed class EmulatorInstance
{
    public Rom Rom { get; }
    public Process Process { get; }
    public DateTime OpenedAt { get; }

    public EmulatorInstance(Rom rom, Process process, DateTime openedAt)
    {
        Rom = rom;
        Process = process;
        OpenedAt = openedAt;
    }
}