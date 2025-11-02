using Emulator;
using Microsoft.VisualBasic;
using System;
using System.Diagnostics;

namespace Pandowdy.Core;

/// <summary>
/// VA2M-specific Bus that owns the CPU connection and routes reads/writes to VA2MMemory.
/// </summary>
public sealed class VA2MBus(VA2MMemory ram, VA2MMemory auxram, VA2MMemory ROM) : IBus
{
    private CPU? _cpu;
    private ulong _systemClock;
    public IMemory RAM => ram; // IMemory-typed view of VA2MMemory
    private IMemory AuxRAM => auxram;
    private IMemory Rom => ROM;
    public ulong SystemClockCounter => _systemClock;

    public void Connect(CPU cpu)
    {
        _cpu = cpu;
        _cpu.Connect(this);
    }

    public byte CpuRead(ushort address, bool readOnly = false)
    {
        if (address < 0xC000)
        {
            return RAM.Read(address);
        }
        else if (address < 0xC100)
        {
            if (address == 0xc010)
            {
                var keyval = auxram.Read(0xc000);
                auxram.Write(0xc000, (byte) (keyval & 0x7F)); // clear high bit on read of strobe
            }
            return RAM.Read(address);
        }
        else
        {

            return ROM.Read(address);
        }

    }

    public void CpuWrite(ushort address, byte data)
    {
        if (address >= 0xD000)
        {
            // ROM area is not writable
            return;
        }
        else if (address >= 0xC000)
        {
            if (address == 0xC010)
            {
                var keyval = auxram.Read(0xc000);
                ram.Write(0xc000, (byte) (keyval & 0x7F)); // clear high bit on read of strobe
                return;
            }
            ram.Write(address, data);
        }
        else
        {
            ram.Write(address, data);
        }
    }

    private int lastPc = 0;

    public void Clock()
    {

        var currPc = _cpu!.PC;

        if (lastPc != currPc)
        {
            if (currPc == 0xd805)
            {

                var lineNum = CpuRead(0x75) + (CpuRead(0x76) * 256);
                // Write curr PC to debug output window
                if (lineNum < 0xFF00)
                {
                    Debug.WriteLine($"LineNum: {lineNum}");
                }
                else
                {
                    Debug.WriteLine($"LineNum: IMMEDIATE");
                }

            }
            else if (currPc == 0xd766)
            {
                Debug.WriteLine("   FOR");
            }
            else if (currPc == 0xDCF9)
            {
                Debug.WriteLine("   NEXT");
            }
        }
        lastPc = currPc;
        _cpu!.Clock();
        _systemClock++;
    }

    public void Reset()
    {
        _cpu!.Reset();
        _systemClock = 0;
    }
}
