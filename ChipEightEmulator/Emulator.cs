using Raylib_cs;

namespace ChipEightEmulator;

public sealed class Emulator
{
    private const int RomStart = 0x0200;
    private const int StackBottom = 0x0EA0;
    private const int ScreenBufferSize = 64 * 32;

    private readonly byte[] _font =
    [
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80, // F
    ];

    private readonly byte[] _cpuRegisters = new byte[16];
    private short _addressRegister;

    private int _instructionPointer = RomStart;
    private int _stackPointer = StackBottom;

    private readonly bool[] _keys = new bool[16];
    private readonly byte[] _memory = new byte[4096];

    private readonly Timer _gameTimer = new(600, true);
    private readonly Timer _delayTimer = new(60);
    private readonly Timer _soundTimer = new(60);

    private readonly byte[] _screenBuffer = new byte[ScreenBufferSize];

    public Emulator(byte[] rom)
    {
        Array.Copy(rom, 0, _memory, RomStart, rom.Length);
        Array.Copy(_font, 0, _memory, 0, _font.Length);
    }

    private byte FlagRegister
    {
        set => _cpuRegisters[^1] = value;
    }

    public unsafe void FillTexture(Texture2D texture, Color color)
    {
        var pixels = stackalloc Color[ScreenBufferSize];

        for (var i = 0; i < ScreenBufferSize; i++)
        {
            if (_screenBuffer[i] == 1)
            {
                pixels[i] = color;
            }
        }

        Raylib.UpdateTexture(texture, pixels);
    }

    public void RunCycle()
    {
        var instruction = ReadInstruction();
        var instructionHeader = RetrieveBits(instruction, 0, 4);

        UpdateKeys();

        switch (instructionHeader)
        {
            case 0x0:
            {
                Process0Instructions(instruction);
                break;
            }

            // 0x1NNN
            // goto NNN;
            case 0x1:
            {
                var address = RetrieveBits(instruction, 4, 12);
                _instructionPointer = address;
                break;
            }

            // 0x2NNN
            // call subroutine at NNN
            case 0x2:
            {
                var address = RetrieveBits(instruction, 4, 12);
                PushReturnPointToStack();
                _instructionPointer = address;
                break;
            }

            // 0x3XNN
            // if (Vx == NN) then skip instruction
            case 0x3:
            {
                var x = RetrieveBits(instruction, 4, 4);
                var value = RetrieveBits(instruction, 8, 8);

                if (_cpuRegisters[x] == value)
                {
                    SkipInstruction();
                }

                break;
            }

            // 0x4XNN
            // if (Vx != NN) then skip instruction
            case 0x4:
            {
                var x = RetrieveBits(instruction, 4, 4);
                var value = RetrieveBits(instruction, 8, 8);

                if (_cpuRegisters[x] != value)
                {
                    SkipInstruction();
                }

                break;
            }

            // 0x5XY0
            // if (Vx == Vy) then skip instruction
            case 0x5:
            {
                var x = RetrieveBits(instruction, 4, 4);
                var y = RetrieveBits(instruction, 8, 4);

                if (_cpuRegisters[x] == _cpuRegisters[y])
                {
                    SkipInstruction();
                }

                break;
            }

            // 0x6XNN
            // Vx = NN
            case 0x6:
            {
                var x = RetrieveBits(instruction, 4, 4);
                var value = RetrieveBits(instruction, 8, 8);
                _cpuRegisters[x] = (byte)value;
                break;
            }

            // 0x7XNN
            // Vx += NN
            case 0x7:
            {
                var x = RetrieveBits(instruction, 4, 4);
                var value = RetrieveBits(instruction, 8, 8);
                _cpuRegisters[x] += (byte)value;
                break;
            }

            case 0x8:
            {
                Process8Instructions(instruction);
                break;
            }

            // 0x9XY0
            // if (Vx != Vy) then skip next instruction
            case 0x9:
            {
                var x = RetrieveBits(instruction, 4, 4);
                var y = RetrieveBits(instruction, 8, 4);

                if (_cpuRegisters[x] != _cpuRegisters[y])
                {
                    SkipInstruction();
                }

                break;
            }

            // 0xANNN
            // I = NNN
            case 0xA:
            {
                var address = RetrieveBits(instruction, 4, 12);
                _addressRegister = (short)address;
                break;
            }

            // 0xBNNN
            // IP = V0 + NNN
            case 0xB:
            {
                var address = RetrieveBits(instruction, 4, 12);
                _instructionPointer = _cpuRegisters[0] + address;
                break;
            }

            // 0xCXNN
            // Vx = rand() & NN (rand is 0 to 255)
            case 0xC:
            {
                var x = RetrieveBits(instruction, 4, 4);
                var mask = RetrieveBits(instruction, 8, 8);
                _cpuRegisters[x] = (byte)(Random.Shared.Next(0, 255) & mask);
                break;
            }

            // 0xDXYN
            // draw(Vx, Vy, N)
            case 0xD:
            {
                var x = RetrieveBits(instruction, 4, 4);
                var y = RetrieveBits(instruction, 8, 4);
                var height = RetrieveBits(instruction, 12, 4);

                var vx = _cpuRegisters[x];
                var vy = _cpuRegisters[y];

                for (var i = 0; i < height; i++)
                {
                    var row = _memory[_addressRegister + i];

                    for (var j = 0; j < 8; j++)
                    {
                        var bit = (byte)((row >> (7 - j)) & 1);
                        SetPixel(vx + j, vy + i, bit);
                    }
                }

                break;
            }
            case 0xE:
            {
                ProcessEInstructions(instruction);
                break;
            }
            case 0xF:
            {
                ProcessFInstructions(instruction);
                break;
            }
            default:
            {
                throw new NotImplementedException(instruction.ToString("X4"));
            }
        }

        UpdateTimers();
    }

    private void UpdateTimers()
    {
        _gameTimer.Update();

        if (_gameTimer.Ticks % 10 != 0)
        {
            return;
        }

        _delayTimer.Update();
        _soundTimer.Update();
    }

    private void Process0Instructions(int instruction)
    {
        switch (instruction)
        {
            // 0x00E0
            // clear screen
            case 0x00E0:
            {
                Array.Clear(_screenBuffer);
                break;
            }

            // 0x00EE
            // return;
            case 0x00EE:
            {
                var address = PopReturnPointFromStack();
                _instructionPointer = address;
                break;
            }

            default:
            {
                throw new NotImplementedException(instruction.ToString("X4"));
            }
        }
    }

    private void Process8Instructions(int instruction)
    {
        var instructionEnd = RetrieveBits(instruction, 12, 4);
        var x = RetrieveBits(instruction, 4, 4);
        var y = RetrieveBits(instruction, 8, 4);

        switch (instructionEnd)
        {
            // 0x8XY0
            // Vx = Vy
            case 0x0:
            {
                _cpuRegisters[x] = _cpuRegisters[y];
                break;
            }

            // 0x8XY1
            // Vx |= Vy
            case 0x1:
            {
                _cpuRegisters[x] |= _cpuRegisters[y];
                break;
            }

            // 0x8XY2
            // Vx &= Vy
            case 0x2:
            {
                _cpuRegisters[x] &= _cpuRegisters[y];
                break;
            }

            // 0x8XY3
            // Vx ^= Vy
            case 0x3:
            {
                _cpuRegisters[x] ^= _cpuRegisters[y];
                break;
            }

            // 0x8XY4
            // Vx += Vy
            case 0x4:
            {
                try
                {
                    _cpuRegisters[x] = (byte)checked(_cpuRegisters[x] + _cpuRegisters[y]);
                }
                catch (OverflowException)
                {
                    FlagRegister = 1;
                }

                break;
            }

            // 0x8XY5
            // Vx -= Vy
            case 0x5:
            {
                FlagRegister = (byte)(_cpuRegisters[x] >= _cpuRegisters[y] ? 1 : 0);
                _cpuRegisters[x] -= _cpuRegisters[y];
                break;
            }

            // 0x8XY6
            // Vx >>= 1
            case 0x6:
            {
                FlagRegister = (byte)(_cpuRegisters[x] & 1);
                _cpuRegisters[x] >>= 1;
                break;
            }

            // 0x8XY7
            // Vx = Vy - Vx
            case 0x7:
            {
                FlagRegister = (byte)(_cpuRegisters[y] >= _cpuRegisters[x] ? 1 : 0);
                _cpuRegisters[x] = (byte)(_cpuRegisters[y] - _cpuRegisters[x]);
                break;
            }

            // 0x8XYE
            // Vx <<= 1
            case 0xE:
            {
                FlagRegister = (byte)((_cpuRegisters[x] >> 7) & 1);
                _cpuRegisters[x] <<= 1;
                break;
            }

            default:
            {
                throw new NotImplementedException(instruction.ToString("X4"));
            }
        }
    }

    private void ProcessEInstructions(int instruction)
    {
        var instructionEnd = RetrieveBits(instruction, 8, 8);
        var x = RetrieveBits(instruction, 4, 4);

        switch (instructionEnd)
        {
            // 0xEX9E
            // if (key() == Vx) then skip next instruction
            case 0x9E:
            {
                if (_keys[_cpuRegisters[x]])
                {
                    SkipInstruction();
                }

                break;
            }

            // 0xEXA1
            // if (key() != Vx) then skip next instruction
            case 0xA1:
            {
                if (_keys[_cpuRegisters[x]] is false)
                {
                    SkipInstruction();
                }

                break;
            }

            default:
            {
                throw new NotImplementedException(instruction.ToString("X4"));
            }
        }
    }

    private void ProcessFInstructions(int instruction)
    {
        var instructionEnd = RetrieveBits(instruction, 8, 8);
        var x = RetrieveBits(instruction, 4, 4);

        switch (instructionEnd)
        {
            // 0xFX07
            // Vx = get_delay()
            case 0x07:
            {
                _cpuRegisters[x] = (byte)_delayTimer.Ticks;
                break;
            }

            // 0xFX0A
            // Vx = get_key()
            case 0x0A:
            {
                while (_keys.All(state => state is false))
                {
                    UpdateKeys();
                }

                _cpuRegisters[x] = (byte)Array.FindIndex(_keys, state => state);

                break;
            }

            // 0xFX15
            // delay_timer(Vx)
            case 0x15:
            {
                _delayTimer.Ticks = x;
                break;
            }

            // 0xFX18
            // sound_timer(Vx)
            case 0x18:
            {
                _soundTimer.Ticks = x;
                break;
            }

            // 0xFX1E
            // I += Vx
            case 0x1E:
            {
                _addressRegister += _cpuRegisters[x];
                break;
            }

            // 0xFX29
            // I = sprite_addr[Vx]
            case 0x29:
            {
                _addressRegister = (short)(_cpuRegisters[x] * 5);
                break;
            }

            // 0xFX33
            // set_BCD(Vx)
            // *(I+0) = BCD(3);
            // *(I+1) = BCD(2);
            // *(I+2) = BCD(1);
            case 0x33:
            {
                var number = _cpuRegisters[x];
                var hundreds = (byte)(number / 100 % 10);
                var tens = (byte)(number / 10 % 10);
                var ones = (byte)(number % 10);

                _memory[_addressRegister] = hundreds;
                _memory[_addressRegister + 1] = tens;
                _memory[_addressRegister + 2] = ones;

                break;
            }

            // 0xFX55
            // reg_dump(Vx, &I)
            case 0x55:
            {
                Array.Copy(_cpuRegisters, 0, _memory, _addressRegister, x + 1);
                break;
            }

            // 0xFX65
            // reg_load(Vx, &I)
            case 0x65:
            {
                Array.Copy(_memory, _addressRegister, _cpuRegisters, 0, x + 1);
                break;
            }

            default:
            {
                throw new NotImplementedException(instruction.ToString("X4"));
            }
        }
    }

    private void UpdateKeys()
    {
        _keys[0x1] = Raylib.IsKeyDown(KeyboardKey.One);
        _keys[0x2] = Raylib.IsKeyDown(KeyboardKey.Two);
        _keys[0x3] = Raylib.IsKeyDown(KeyboardKey.Three);
        _keys[0xC] = Raylib.IsKeyDown(KeyboardKey.Four);

        _keys[0x4] = Raylib.IsKeyDown(KeyboardKey.Q);
        _keys[0x5] = Raylib.IsKeyDown(KeyboardKey.W);
        _keys[0x6] = Raylib.IsKeyDown(KeyboardKey.E);
        _keys[0xD] = Raylib.IsKeyDown(KeyboardKey.R);

        _keys[0x7] = Raylib.IsKeyDown(KeyboardKey.A);
        _keys[0x8] = Raylib.IsKeyDown(KeyboardKey.S);
        _keys[0x9] = Raylib.IsKeyDown(KeyboardKey.D);
        _keys[0xE] = Raylib.IsKeyDown(KeyboardKey.F);

        _keys[0xA] = Raylib.IsKeyDown(KeyboardKey.Z);
        _keys[0x0] = Raylib.IsKeyDown(KeyboardKey.X);
        _keys[0xB] = Raylib.IsKeyDown(KeyboardKey.C);
        _keys[0xF] = Raylib.IsKeyDown(KeyboardKey.V);
    }

    private void SetPixel(int x, int y, byte value)
    {
        x %= 64;
        y %= 32;

        var index = y * 64 + x;

        if (_screenBuffer[index] == 1 && value == 1)
        {
            FlagRegister = 1;
        }
        else
        {
            FlagRegister = 0;
        }

        _screenBuffer[index] ^= value;
    }

    private static int RetrieveBits(int instruction, int offset, int length)
    {
        var mask = (1 << length) - 1;
        return (instruction >> (16 - (offset + length))) & mask;
    }

    private int ReadInstruction()
    {
        var opcode = (_memory[_instructionPointer] << 8) | _memory[_instructionPointer + 1];
        _instructionPointer += 2;
        return opcode;
    }

    private void SkipInstruction()
    {
        _instructionPointer += 2;
    }

    private void PushReturnPointToStack()
    {
        const int stackTop = 0x0EA0 + 48;

        if (_stackPointer >= stackTop)
        {
            throw new StackOverflowException();
        }

        var bytes = BitConverter.GetBytes(_instructionPointer);

        for (var i = 0; i < bytes.Length; i++)
        {
            _memory[_stackPointer + i] = bytes[i];
        }

        _stackPointer += 4;
    }

    private int PopReturnPointFromStack()
    {
        const int stackBottom = 0x0EA0;

        if (_stackPointer <= stackBottom)
        {
            throw new InvalidOperationException("Call stack is empty");
        }

        var returnPoint = BitConverter.ToInt32(_memory, _stackPointer - 4);

        _stackPointer -= 4;

        return returnPoint;
    }
}