using System.Timers;
using Raylib_cs;

public class Timer : IDisposable
{
    public event Action? Tick;

    private readonly System.Timers.Timer _timer;

    public Timer()
    {
        _timer = new System.Timers.Timer(TimeSpan.FromMilliseconds(16));
        _timer.Enabled = true;
        _timer.AutoReset = true;
        _timer.Elapsed += OnTimerTick;
    }

    public int TicksLeft { get; set; } = 60;

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        TicksLeft--;

        if (TicksLeft < 0)
        {
            TicksLeft = 60;
        }

        Tick?.Invoke();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}

public class Emulator : IDisposable
{
    private readonly ushort[] _cpuRegisters = new ushort[16];
    private ushort _addressRegister = 0;

    private int _instructionPointer = 0x200;
    private int _stackPointer = 0xEA0;

    private readonly byte[] _memory = new byte[4096];
    private readonly Timer _delayTimer = new();
    private readonly Timer _soundTimer = new();

    public Emulator(byte[] rom)
    {
        Array.Copy(rom, 0, _memory, 512, rom.Length);
    }

    public void Start()
    {
        while (true)
        {
            var instruction = ReadInstruction();
            var instructionHeader = RetrieveBits(instruction, 0, 4);

            switch (instructionHeader)
            {
                case 0x0:
                    Process0Instructions(instruction);
                    break;
                case 0x1:
                    // 0x1NNN
                    // goto NNN;
                    break;
                case 0x2:
                    // 0x2NNN
                    // call subroutine at NNN
                    break;
                case 0x3:
                    // 0x3XNN
                    // if (Vx == NN) then skip instruction
                    break;
                case 0x4:
                    // 0x4XNN
                    // if (Vx != NN) then skip instruction
                    break;
                case 0x5:
                    // 0x5XY0
                    // if (Vx == Vy)
                    break;
                case 0x6:
                    // 0x6XNN
                    // Vx = NN
                    break;
                case 0x7:
                    // 0x7XNN
                    // Vx += NN
                    break;
                case 0x8:
                    Process8Instructions(instruction);
                    break;
                case 0x9:
                    // 0x9XY0
                    // if (Vx != Vy) then skip next instruction
                    break;
                case 0xA:
                    // 0xANNN
                    // I = NNN
                    break;
                case 0xB:
                    // 0xBNNN
                    // IP = V0 + NNN
                    break;
                case 0xC:
                    // 0xCXNN
                    // Vx = rand() & NN (rand is 0 to 255)
                    break;
                case 0xD:
                    // 0xDXYN
                    // draw(Vx, Vy, N)
                    break;
                case 0xE:
                    ProcessEInstructions(instruction);
                    break;
            }
        }
    }

    private void Process0Instructions(int instruction)
    {
        switch (instruction)
        {
            case 0x00E0:
                // 0x00E0
                // clear screen
                break;
            case 0x00EE:
                // 0x00EE
                // return;
                break;
        }
    }

    private void Process8Instructions(int instruction)
    {
        var instructionEnd = RetrieveBits(instruction, 12, 4);
        var x = RetrieveBits(instruction, 4, 4);
        var y = RetrieveBits(instruction, 8, 4);

        switch (instructionEnd)
        {
            case 0x0:
                // 0x8XY0
                // Vx = Vy
                break;
            case 0x1:
                // 0x8XY1
                // Vx = Vy
                break;
            case 0x2:
                // 0x8XY2
                // Vx &= Vy
                break;
            case 0x3:
                // 0x8XY3
                // Vx ^= Vy
                break;
            case 0x4:
                // 0x8XY4
                // Vx += Vy
                break;
            case 0x5:
                // 0x8XY5
                // Vx -= Vy
                break;
            case 0x6:
                // 0x8XY6
                // Vx >>= 1
                break;
            case 0x7:
                // 0x8XY7
                // Vx = Vy - Vx
                break;
            case 0xE:
                // 0x8XYE
                // Vx <<= 1
                break;
        }
    }

    private void ProcessEInstructions(int instruction)
    {
        var instructionEnd = RetrieveBits(instruction, 8, 8);
        var x = RetrieveBits(instruction, 4, 4);

        switch (instructionEnd)
        {
            case 0x9E:
                // 0xEX9E
                // if (key() == Vx) then skip next instruction
                break;
            case 0xA1:
                // 0xA1
                // if (key() != Vx) then skip next instruction
                break;
        }
    }

    private void ProcessFInstructions(int instruction)
    {
        var instructionEnd = RetrieveBits(instruction, 8, 8);
        var x = RetrieveBits(instruction, 4, 4);

        switch (instructionEnd)
        {
            case 0x07:
                // 0xFX07
                break;
            case 0x0A:
                // 0xFX0A
                break;
            case 0x15:
                // 0xFX15
                break;
            case 0x18:
                // 0xFX18
                break;
            case 0x1E:
                // 0xFX1E
                break;
            case 0x29:
                // 0xFX29
                break;
            case 0x33:
                // 0xFX33
                break;
            case 0x55:
                // 0xFX55
                break;
            case 0x65:
                // 0xFX65
                break;
        }
    }

    private static int RetrieveBits(int instruction, int offset, int length)
    {
        var mask = (1 << length) - 1;
        return (instruction >> (16 - (offset + length))) & mask;
    }

    private int ReadInstruction()
    {
        var left = _memory[_instructionPointer + 1];
        var right = _memory[_instructionPointer];
        _instructionPointer += 2;
        return (left << 8) | right;
    }

    public void Dispose()
    {
        _delayTimer.Dispose();
        _soundTimer.Dispose();
    }
}

public class Program
{
    private const string _romPath = "danm8ku.ch8";

    public static void Main(string[] args)
    {
        var bytes = File.ReadAllBytes(_romPath);
        var emulator = new Emulator(bytes);
        emulator.Start();
    }

    private static void RenderScreen()
    {
        Raylib.InitWindow(64, 32, "Chip-8 Emulator");

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Color.Black);

            Raylib.DrawText("Hello, world!", 12, 12, 20, Color.White);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}