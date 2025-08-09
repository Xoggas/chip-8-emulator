using System.Diagnostics;
using ChipEightEmulatorGUI.Models;

namespace ChipEightEmulatorGUI.Services;

public sealed class EmulatorService
{
    private const string EmulatorExeName = "ChipEightEmulator.exe";

    private readonly Dictionary<Rom, Process> _processes = [];
    private readonly RomStorageService _romStorageService;

    public EmulatorService(RomStorageService romStorageService)
    {
        _romStorageService = romStorageService;
    }

    public IReadOnlyDictionary<Rom, Process> Processes => _processes;

    public Process? RunRom(Rom rom)
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

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => UnregisterProcessForRom(rom);

        _processes.Add(rom, process);

        return process;
    }

    public void StopRom(Rom rom)
    {
        var process = _processes[rom];

        process.Kill();
    }

    private void UnregisterProcessForRom(Rom rom)
    {
        _processes.Remove(rom);
    }
}