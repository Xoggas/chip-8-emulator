using System.Globalization;
using System.Numerics;
using ChipEightEmulator;
using Raylib_cs;

public static class ChipEightEmulatorRuntime
{
    private const int Scale = 15;

    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            throw new ArgumentException("ROM path is empty!");
        }

        var romPath = args[0];

        RunGame(romPath);
    }

    private static void RunGame(string romPath)
    {
        Raylib.InitWindow(64 * Scale, 32 * Scale, "Chip-8 Emulator");

        var blankImage = Raylib.GenImageColor(64, 32, Color.Black);
        var screenTexture = Raylib.LoadTextureFromImage(blankImage);
        var emulator = new Emulator(File.ReadAllBytes(romPath));
        var sourceRect = new Rectangle(0, 0, 64, 32);
        var destinationRect = new Rectangle(0, 0, 64 * Scale, 32 * Scale);

        while (Raylib.WindowShouldClose() == false)
        {
            emulator.RunCycle();
            emulator.FillTexture(screenTexture, Color.Green);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawTexturePro(screenTexture, sourceRect, destinationRect, Vector2.Zero, 0, Color.White);
            Raylib.EndDrawing();

            Thread.Sleep(TimeSpan.FromSeconds(1f / 600f));
        }

        Raylib.UnloadImage(blankImage);
        Raylib.UnloadTexture(screenTexture);
        Raylib.CloseWindow();
    }
}