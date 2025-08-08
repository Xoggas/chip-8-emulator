using System.Numerics;
using ChipEightEmulator;
using Raylib_cs;

public static class Program
{
    private const int Scale = 15;
    private const string RomPath = "pong.ch8";

    public static void Main(string[] args)
    {
        Raylib.InitWindow(64 * Scale, 32 * Scale, "Chip-8 Emulator");

        var screenTexture = Raylib.LoadTextureFromImage(Raylib.GenImageColor(64, 32, Color.Black));
        var emulator = new Emulator(File.ReadAllBytes(RomPath));

        while (Raylib.WindowShouldClose() == false)
        {
            emulator?.RunCycle();
            emulator?.FillTexture(screenTexture, Color.Green);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawTexturePro(screenTexture, new Rectangle(0, 0, 64, 32),
                new Rectangle(0, 0, 64 * Scale, 32 * Scale),
                Vector2.Zero, 0, Color.White);
            Raylib.EndDrawing();

            Thread.Sleep(TimeSpan.FromSeconds(1f / 600f));
        }

        Raylib.UnloadTexture(screenTexture);
        Raylib.CloseWindow();
    }
}