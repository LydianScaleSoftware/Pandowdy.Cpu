using System;
using System.Diagnostics;
using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore;

/// <summary>
/// Implements a Disk II floppy disk drive with mechanical head positioning and motor control.
/// </summary>
/// <remarks>
/// <para>
/// This class simulates the physical characteristics of a Disk II drive:
/// <list type="bullet">
/// <item>Stepper motor with quarter-track positioning (0-139 quarter-tracks = 0-34.75 whole tracks)</item>
/// <item>Motor on/off control</item>
/// <item>Read/write operations through an <see cref="IDiskImageProvider"/></item>
/// </list>
/// </para>
/// <para>
/// The drive delegates actual bit reading/writing to the <see cref="IDiskImageProvider"/>,
/// while managing the mechanical aspects (motor state, head position). This separation
/// matches real hardware where the drive mechanism is separate from the disk media.
/// </para>
/// </remarks>
public class DiskIIDrive : IDiskIIDrive
{
    private const int MAX_QUARTER_STEPS = 35 * 4 + 1; // 35 tracks + 1/2, 1/2, and 3/4 between Tracks 0 and 35.
    private const int DEFAULT_START_QUARTER_TRACK = 4 * 17; // Start at track 17 (typical boot track area)

    private IDiskImageProvider? _imageProvider;
    private readonly IDiskImageFactory? _diskImageFactory;
    private int _quarterSteps = 0;
    private bool _motorOn = false;
    private bool _hitMinLogged = false;
    private bool _hitMaxLogged = false;

    /// <summary>
    /// Gets the name of this drive for identification and debugging purposes.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskIIDrive"/> class.
    /// </summary>
    /// <param name="name">Name for the drive (e.g., "Slot6-D1").</param>
    /// <param name="imageProvider">
    /// Optional disk image provider. If null, the drive behaves as if no disk is inserted.
    /// </param>
    /// <param name="diskImageFactory">
    /// Optional factory for creating disk image providers when inserting disks.
    /// </param>
    public DiskIIDrive(string name, IDiskImageProvider? imageProvider = null, IDiskImageFactory? diskImageFactory = null)
    {
        Name = name ?? "Unnamed";
        _imageProvider = imageProvider;
        _diskImageFactory = diskImageFactory;
        Reset();

        // Notify image provider of initial track position
        _imageProvider?.SetQuarterTrack(_quarterSteps);
    }

    /// <summary>
    /// Inserts a disk image into this drive.
    /// </summary>
    /// <param name="diskImagePath">Path to the disk image file.</param>
    /// <exception cref="InvalidOperationException">Thrown if no disk image factory is available.</exception>
    public void InsertDisk(string diskImagePath)
    {
        if (_diskImageFactory == null)
        {
            throw new InvalidOperationException("Cannot insert disk: no disk image factory available");
        }

        // Eject current disk if any
        EjectDisk();

        // Load new disk
        _imageProvider = _diskImageFactory.CreateProvider(diskImagePath);
        _imageProvider.SetQuarterTrack(_quarterSteps);

        Debug.WriteLine($"Drive '{Name}': Inserted disk '{diskImagePath}'");
    }

    /// <summary>
    /// Ejects the current disk from this drive.
    /// </summary>
    public void EjectDisk()
    {
        if (_imageProvider != null)
        {
            // Flush any pending writes
            _imageProvider.Flush();

            // Dispose if the provider is disposable
            if (_imageProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _imageProvider = null;
            Debug.WriteLine($"Drive '{Name}': Ejected disk");
        }
    }

    /// <summary>
    /// Gets whether a disk is currently inserted in this drive.
    /// </summary>
    public bool HasDisk => _imageProvider != null;

    /// <summary>
    /// Resets the drive to power-on state.
    /// </summary>
    /// <remarks>
    /// Turns off the motor and moves the head to the default starting position (track 17).
    /// This matches real Disk II behavior where the head position is maintained across resets.
    /// </remarks>
    public void Reset()
    {
        _quarterSteps = DEFAULT_START_QUARTER_TRACK;
        MotorOn = false;
    }

    /// <summary>
    /// Gets or sets the motor state.
    /// </summary>
    /// <remarks>
    /// The motor must be on to read or write data. When the motor turns on or off,
    /// a debug message is logged.
    /// </remarks>
    public bool MotorOn
    {
        get => _motorOn;
        set
        {
            if (_motorOn != value)
            {
                _motorOn = value;
                Debug.WriteLine($"Drive motor turned {(value ? "ON" : "OFF")}");
            }
        }
    }

    /// <summary>
    /// Gets the current track position as a whole and fractional part.
    /// </summary>
    /// <returns>
    /// A tuple where:
    /// <list type="bullet">
    /// <item>track = whole track number (0-34)</item>
    /// <item>quarter = fractional part (0-3, representing 0/4, 1/4, 2/4, 3/4)</item>
    /// </list>
    /// </returns>
    public double Track
    {
        get => _quarterSteps / 4.0;
    }

    /// <summary>
    /// Gets the raw quarter-track position (0-139) used for stepper motor calculations.
    /// </summary>
    public int QuarterTrack => _quarterSteps;

    /// <summary>
    /// Moves the head one quarter-track toward higher track numbers (0→34).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Movement is clamped at track 34.75 (quarter-track 139). Attempting to move beyond
    /// this limit will keep the head at the maximum position.
    /// </para>
    /// <para>
    /// After moving, the <see cref="IDiskImageProvider"/> is notified of the new track position.
    /// </para>
    /// </remarks>
    public void StepToHigherTrack()
    {
        _quarterSteps++;
        if (_quarterSteps >= MAX_QUARTER_STEPS)
        {
            _quarterSteps = MAX_QUARTER_STEPS - 1;
            if (!_hitMaxLogged)
            {
                Debug.WriteLine($"Drive '{Name}' head hit maximum position at quarter-track {_quarterSteps}");
                _hitMaxLogged = true;
            }
        }
        else
        {
            _hitMaxLogged = false;
        }

        _hitMinLogged = false;
        // Notify image provider of track change
        _imageProvider?.SetQuarterTrack(_quarterSteps);
    }

    /// <summary>
    /// Moves the head one quarter-track toward lower track numbers (34→0).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Movement is clamped at track 0 (quarter-track 0). Attempting to move beyond
    /// this limit will keep the head at the minimum position.
    /// </para>
    /// <para>
    /// After moving, the <see cref="IDiskImageProvider"/> is notified of the new track position.
    /// </para>
    /// </remarks>
    public void StepToLowerTrack()
    {
        _quarterSteps--;
        if (_quarterSteps < 0)
        {
            _quarterSteps = 0;
            if (!_hitMinLogged)
            {
                Debug.WriteLine($"Drive '{Name}' head hit minimum position at quarter-track 0");
                _hitMinLogged = true;
            }
        }
        else
        {
            _hitMinLogged = false;
        }

        _hitMaxLogged = false;
        // Notify image provider of track change
        _imageProvider?.SetQuarterTrack(_quarterSteps);
    }

    /// <summary>
    /// Reads the next bit from the disk based on the current CPU cycle count.
    /// </summary>
    /// <param name="currentCycle">Current CPU cycle count for timing calculations.</param>
    /// <returns>
    /// The next bit value (true/false), or null if:
    /// <list type="bullet">
    /// <item>No disk is inserted (no image provider)</item>
    /// <item>Motor is off</item>
    /// <item>Not enough time has elapsed for the next bit</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// The motor must be on to read data. If the motor is off or no disk is inserted,
    /// this method returns null, which the controller interprets as no data available.
    /// </remarks>
    public bool? GetBit(ulong currentCycle)
    {
        if (!_motorOn)
        {
            return null;
        }

        if (_imageProvider == null)
        {
            return null;
        }

        return _imageProvider.GetBit(currentCycle);
    }

    /// <summary>
    /// Writes a bit to the disk at the current position.
    /// </summary>
    /// <param name="value">The bit value to write (true = 1, false = 0).</param>
    /// <returns>
    /// True if the write was successful; false if:
    /// <list type="bullet">
    /// <item>No disk is inserted</item>
    /// <item>Motor is off</item>
    /// <item>Disk is write-protected</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Write operations delegate to the image provider. The provider may reject
    /// the write if the disk is write-protected or the format doesn't support writes.
    /// </remarks>
    public bool SetBit(bool value)
    {
        if (!_motorOn || _imageProvider == null)
        {
            return false;
        }

        // Delegate to image provider (will return false if write-protected)
        return _imageProvider.WriteBit(value, 0); // cycleCount not used yet
    }

    /// <summary>
    /// Gets the write-protection status of the disk.
    /// </summary>
    /// <returns>
    /// True if the disk is write-protected; false if writes are allowed.
    /// Returns false if no disk is inserted.
    /// </returns>
    /// <remarks>
    /// This delegates to the image provider's IsWriteProtected property.
    /// </remarks>
    public bool IsWriteProtected()
    {
        // No disk inserted = not write protected (controller sees no disk)
        if (_imageProvider == null)
        {
            return false;
        }

        // Delegate to image provider
        return _imageProvider.IsWriteProtected;
    }
}
