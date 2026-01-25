
namespace Pandowdy.EmuCore.Interfaces;

public interface IDiskIIDrive // Will take a reference to CpuClockingCounters and a DiskImageProvider which will return a bit stream from a disk image/track
{
    public string Name { get; } // Drive identification name for debugging and logging

    public void Reset();  // Turn Motor off. Leave head position where it is.

    public bool MotorOn { get; set; } // Motor must be true in order to read or write values.

    public double Track { get; }  // Returns a whole and fractional part of a track

    public int QuarterTrack { get; } // Returns the raw quarter-track position (0-139) for stepper motor calculations

    public void StepToHigherTrack();  // Move head toward higher track numbers (0->34) in quarter-track increments

    public void StepToLowerTrack(); // Move head toward lower track numbers (34->0) in quarter-track increments

    public bool? GetBit(ulong currentCycle); // Returns value of next bit based on cycle timing, or null if no disk/can't read

    public bool SetBit(bool value); // Sets bit.  Returns true on success, false on fail/can't write

    public bool IsWriteProtected();

    // Disk management operations
    public void InsertDisk(string diskImagePath); // Insert a disk image into the drive
    public void EjectDisk(); // Eject the current disk from the drive
    public bool HasDisk { get; } // Returns true if a disk is currently inserted
}


