using System.Diagnostics;
using System.Reflection;
using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore;



/// <summary>
/// Decorator for IDiskDrive that adds debug logging to all operations.
/// </summary>
public class DiskIIDebugDecorator : IDiskIIDrive
{
    private readonly IDiskIIDrive _inner;

    public int QuarterTrack => _inner.QuarterTrack;

    public DiskIIDebugDecorator(IDiskIIDrive inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        Debug.WriteLine($"Creating DebugDiskDecorator for drive '{Name}'");
    }

    /// <summary>
    /// Gets the name from the inner drive.
    /// </summary>
    public string Name => _inner.Name;

    private string MotorString()
    {
        return _inner.MotorOn ? "On" : "Off";
    }

    public void Reset()
    {
        Debug.WriteLine($"IDiskDrive ({Name}): Reset()");
        _inner.Reset();
    }

    public bool MotorOn
    {
        get
        {
            return _inner.MotorOn;
        }
        set
        {
            _inner.MotorOn = value;
            Debug.WriteLine($"IDiskDrive ({Name}) Motor is now {MotorString()}");
        }
    }

    public double Track
    {
        get
        {
            var retval = _inner.Track;
          //  Debug.WriteLine($"IDiskDrive ({Name}) Track = {retval}");
            return retval;
        }
    }

    public void StepToHigherTrack()
        {
            Debug.WriteLine($"IDiskDrive ({Name}) StepToHigherTrack()");
            _inner.StepToHigherTrack();
            var track = _inner.Track;
            if ((int) _inner.Track >= 35)
            {
                Debug.WriteLine($" (Head hit max range at {track})");
            }
        }

    public void StepToLowerTrack()
    {
        Debug.WriteLine($"IDiskDrive ({Name}) StepToLowerTrack()");
        _inner.StepToLowerTrack();
        if (_inner.QuarterTrack == 0)
        {
            Debug.WriteLine(" (Head hit max range at (0,0))");
        }
    }

    public bool? GetBit(ulong currentCycle)
    {
        var val = _inner.GetBit(currentCycle);
        // Disabled to reduce spam - uncomment if needed for detailed bit debugging
        // if (val == null)
        // {
        //     Debug.WriteLine($"IDiskDrive ({Name}) GetBit(cycle={currentCycle}) => NULL (Motor:{_inner.MotorOn}, Track:{_inner.Track})");
        // }
        // else
        // {
        //     Debug.WriteLine($"IDiskDrive ({Name}) GetBit(cycle={currentCycle}) => {(val.Value ? "1" : "0")}");
        // }
        return val;
    }

    public bool SetBit(bool value)
    {
        Debug.WriteLine($"IDiskDrive ({Name}) SetBit({value})");
        return _inner.SetBit(value);
    }

        public bool IsWriteProtected()
        {
            Debug.WriteLine($"IDiskDrive ({Name}) IsWriteProtected()");
            return _inner.IsWriteProtected();
        }

        public void InsertDisk(string diskImagePath)
        {
            Debug.WriteLine($"IDiskDrive ({Name}) InsertDisk('{diskImagePath}')");
            _inner.InsertDisk(diskImagePath);
        }

        public void EjectDisk()
        {
            Debug.WriteLine($"IDiskDrive ({Name}) EjectDisk()");
            _inner.EjectDisk();
        }

        public bool HasDisk => _inner.HasDisk;
    }
