using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore;

/// <summary>
/// Implements the Apple II No-Slot Clock functionality.
/// The No-Slot Clock uses a bit-banging protocol accessed through $C0n0-$C0nF
/// where reads trigger clock/data operations based on the low nibble of the address.
/// </summary>
public class NoSlotClockIoHandler : ISystemIoHandler
{
    private ISystemIoHandler _downstream;
    private CpuClockingCounters _clockingCounters;

    // No-Slot Clock state
    private bool _isUnlocked;
    private int _unlockSequenceIndex;
    private int _bitPosition;
    private byte _currentByte;
    private ClockMode _mode;
    private bool _writeMode;
    private ulong _lastUnlockAccessTick;

    // The unlock sequence: reading addresses in this specific pattern
    private static readonly byte[] UnlockSequence = { 0x5, 0xA, 0x5, 0xA };
    
    // Timeout for unlock sequence: ~100 cycles between accesses
    // (adjustable based on real hardware behavior)
    private const ulong UnlockTimeoutCycles = 100;

    private enum ClockMode
    {
        Locked,
        ReadClock,
        ReadCompare,
        Write
    }

    public NoSlotClockIoHandler(ISystemIoHandler downstream, CpuClockingCounters clockingCounters)
    {   
        ArgumentNullException.ThrowIfNull(downstream);
        ArgumentNullException.ThrowIfNull(clockingCounters);
        _downstream = downstream;
        _clockingCounters = clockingCounters;
        Reset();
    }

    public void Reset()
    {
        _downstream.Reset();
        _isUnlocked = false;
        _unlockSequenceIndex = 0;
        _bitPosition = 0;
        _currentByte = 0;
        _mode = ClockMode.Locked;
        _writeMode = false;
        _lastUnlockAccessTick = 0ul;
    }

    public int Size
    {
        get { return _downstream.Size; }
    }

    public byte Read(ushort loc)
    {
        byte lowNibble = (byte)(loc & 0x0F);

        // No-Slot Clock detection and operation
        if (lowNibble <= 0x0B)
        {
            return HandleNoSlotClockRead(lowNibble);
        }

        // Pass through to downstream handler
        return _downstream.Read(loc);
    }

    public void Write(ushort loc, byte val)
    {
        byte lowNibble = (byte)(loc & 0x0F);

        // No-Slot Clock write operations
        if (lowNibble <= 0x0B && _isUnlocked)
        {
            HandleNoSlotClockWrite(lowNibble, val);
            return;
        }

        // Pass through to downstream handler
        _downstream.Write(loc, val);
    }

    private byte HandleNoSlotClockRead(byte offset)
    {
        // Check for unlock sequence
        if (!_isUnlocked)
        {
            ulong currentTick = _clockingCounters.TotalCycles;
            
            // Check for timeout between unlock sequence accesses
            if (_unlockSequenceIndex > 0)
            {
                ulong cyclesSinceLastAccess = currentTick - _lastUnlockAccessTick;
                if (cyclesSinceLastAccess > UnlockTimeoutCycles)
                {
                    // Timeout - reset unlock sequence
                    _unlockSequenceIndex = 0;
                }
            }
            
            if (offset == UnlockSequence[_unlockSequenceIndex])
            {
                _unlockSequenceIndex++;
                _lastUnlockAccessTick = currentTick;
                
                if (_unlockSequenceIndex >= UnlockSequence.Length)
                {
                    _isUnlocked = true;
                    _unlockSequenceIndex = 0;
                    _bitPosition = 0;
                    _mode = ClockMode.ReadClock;
                    _writeMode = false;
                }
            }
            else
            {
                // Wrong sequence, reset
                _unlockSequenceIndex = 0;
            }
            return _downstream.Read((ushort)(0xC000 | offset));
        }

        // Once unlocked, handle clock operations
        switch (offset)
        {
            case 0x0: // Read data bit 0
                return ReadClockBit();

            case 0x1: // Shift clock data
                ShiftClockData();
                return 0x00;

            case 0x2: // Enable write mode
                _writeMode = true;
                return 0x00;

            case 0x3: // Disable write mode / Enable read mode
                _writeMode = false;
                return 0x00;

            case 0x4: // Load next byte for reading
                LoadNextClockByte();
                return 0x00;

            case 0x5: // Part of unlock sequence when locked
            case 0xA: // Part of unlock sequence when locked
                return 0x00;

            default:
                // Other addresses don't affect clock state
                return 0x00;
        }
    }

    private void HandleNoSlotClockWrite(byte offset, byte val)
    {
        if (!_writeMode)
        {
            return;
        }

        switch (offset)
        {
            case 0x0: // Write data bit
                WriteClockBit((val & 0x01) != 0);
                break;

            case 0x1: // Shift clock data
                ShiftClockData();
                break;

            case 0x4: // Store current byte to clock
                StoreClockByte();
                break;
        }
    }

    private byte ReadClockBit()
    {
        // Read the current bit from the current byte
        byte bit = (byte)((_currentByte >> _bitPosition) & 0x01);
        return (byte)(bit != 0 ? 0x80 : 0x00); // NSC returns bit in high bit of byte
    }

    private void ShiftClockData()
    {
        _bitPosition++;
        if (_bitPosition >= 8)
        {
            _bitPosition = 0;
            // Auto-load next byte after 8 bits
            if (!_writeMode)
            {
                LoadNextClockByte();
            }
        }
    }

    private void WriteClockBit(bool bitValue)
    {
        if (bitValue)
        {
            _currentByte |= (byte)(1 << _bitPosition);
        }
        else
        {
            _currentByte &= (byte)~(1 << _bitPosition);
        }
    }

    private void LoadNextClockByte()
    {
        // Load the next byte from the clock registers
        // The No-Slot Clock has 8 bytes of time data in BCD format:
        // Byte 0: Centiseconds (00-99)
        // Byte 1: Seconds (00-59)
        // Byte 2: Minutes (00-59)
        // Byte 3: Hours (00-23)
        // Byte 4: Day of week (00-06, 0=Sunday)
        // Byte 5: Day of month (01-31)
        // Byte 6: Month (01-12)
        // Byte 7: Year (00-99)

        int byteIndex = _bitPosition / 8;

        _currentByte = byteIndex switch
        {
            0 => 0x00, // TODO: centiseconds (placeholder)
            1 => 0x00, // TODO: seconds (placeholder)
            2 => 0x00, // TODO: minutes (placeholder)
            3 => 0x12, // TODO: hours (placeholder - 12 noon)
            4 => 0x01, // TODO: day of week (placeholder - Monday)
            5 => 0x15, // TODO: day of month (placeholder - 15th)
            6 => 0x06, // TODO: month (placeholder - June)
            7 => 0x24, // TODO: year (placeholder - 2024 -> 24)
            _ => 0x00
        };

        _bitPosition = 0;
    }

    private void StoreClockByte()
    {
        // Store the current byte to the clock registers
        // This would write to the clock hardware
        // For now, this is a stub - actual date/time setting would go here
        
        int byteIndex = _bitPosition / 8;

        // TODO: Implement actual clock register writes
        // The byte index determines which register to write:
        // 0 = centiseconds, 1 = seconds, 2 = minutes, etc.

        _currentByte = 0;
        _bitPosition = 0;
    }

    public byte this[ushort offset]
    {
        get { return Read(offset); }
        set { Write(offset, value); }
    }
}
