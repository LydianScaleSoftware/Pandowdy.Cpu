
using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore;


/// <summary>
/// Null object implementation of IDiskDrive.
/// Provides a functional but no-op disk drive for testing.
/// </summary>
public class NullDiskIIDrive : IDiskIIDrive
{
    private bool _motor = false;
    private int _quarterSteps = 0;
    private const int MaxSteps = 35 * 4 + 1;

    public string Name { get; }

    public NullDiskIIDrive(string name = "NullDrive")
    {
        Name = name;
        Reset();
    }

    public void Reset()
    {
        _quarterSteps = 4 * 17;
        MotorOn = false;
    }

    public bool MotorOn
    {
        get => _motor;
        set => _motor = value;
    }

    public double Track
    {
        get => _quarterSteps / 4.0;
    }

    public int QuarterTrack => _quarterSteps;

    public static int BitPosition => 0; // Null drive always returns 0

    public void StepToHigherTrack()
    {
        _quarterSteps++;
        if (_quarterSteps > MaxSteps)
        {
            _quarterSteps = MaxSteps;
        }
    }

    public void StepToLowerTrack()
    {
        _quarterSteps--;
        if (_quarterSteps < 0)
        {
            _quarterSteps = 0;
        }
    }

    public bool? GetBit(ulong currentCycle)
    {
        return null;
    }

    public bool SetBit(bool value)
    {
        return false;
    }

        public bool IsWriteProtected()
        {
            return false;
        }

        public void InsertDisk(string diskImagePath)
        {
            // No-op: Null drive doesn't support disk insertion
        }

        public void EjectDisk()
        {
            // No-op: Null drive doesn't have a disk to eject
        }

        public bool HasDisk => false; // Null drive never has a disk
    }
