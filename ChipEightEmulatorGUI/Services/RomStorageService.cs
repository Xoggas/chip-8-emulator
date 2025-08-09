using System.Globalization;
using ChipEightEmulatorGUI.Models;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;

namespace ChipEightEmulatorGUI.Services;

public sealed class RomStorageService
{
    public IReadOnlyList<Rom> Roms => _roms;

    private const string DataFolder = "Data";

    private readonly string _loadedRomsMetadataPath = Path.Combine(DataFolder, "LoadedRoms.json");
    private readonly string _romsFolderPath = Path.Combine(DataFolder, "Roms");
    private readonly List<Rom> _roms = [];

    public IEnumerable<Rom> LoadRoms()
    {
        if (File.Exists(_loadedRomsMetadataPath) is false)
        {
            return [];
        }

        var json = File.ReadAllText(_loadedRomsMetadataPath);
        var roms = JsonConvert.DeserializeObject<IEnumerable<Rom>>(json) ?? [];

        _roms.AddRange(roms);

        return _roms;
    }

    public string GetRomPath(Rom rom)
    {
        return Path.Combine(_romsFolderPath, rom.Guid.ToString());
    }

    public async Task<Rom> RegisterRom(IBrowserFile file)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        var title = textInfo.ToTitleCase(Path.GetFileNameWithoutExtension(file.Name));
        var size = file.Size;
        var guid = Guid.NewGuid();
        var path = Path.Combine(_romsFolderPath, guid.ToString());

        if (Directory.Exists(_romsFolderPath) is false)
        {
            Directory.CreateDirectory(_romsFolderPath);
        }

        await using var readStream = file.OpenReadStream();
        await using var writeStream = File.Create(path);
        await readStream.CopyToAsync(writeStream);

        var rom = new Rom
        {
            Guid = guid,
            Title = title,
            Size = size,
        };

        _roms.Add(rom);

        SaveLoadedRomsList(_roms);

        return rom;
    }

    public void UnregisterRom(Rom rom)
    {
        _roms.Remove(rom);

        var path = Path.Combine(_romsFolderPath, rom.Guid.ToString());

        File.Delete(path);

        SaveLoadedRomsList(_roms);
    }

    private void SaveLoadedRomsList(IEnumerable<Rom> roms)
    {
        if (Directory.Exists(DataFolder) is false)
        {
            Directory.CreateDirectory(DataFolder);
        }

        var json = JsonConvert.SerializeObject(roms);

        File.WriteAllText(_loadedRomsMetadataPath, json);
    }
}