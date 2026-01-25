using Pandowdy.EmuCore.Interfaces;
using Pandowdy.EmuCore.Services;

namespace Pandowdy.EmuCore;

/// <summary>
/// Factory for creating Disk II drives with optional disk images.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Decorator Chain:</strong> Creates drives wrapped in multiple decorators:
/// <code>
/// DiskIIDebugDecorator → DiskIIStatusDecorator → DiskIIDrive
/// </code>
/// - <see cref="DiskIIDrive"/>: Core drive implementation
/// - <see cref="DiskIIStatusDecorator"/>: Synchronizes state with status provider
/// - <see cref="DiskIIDebugDecorator"/>: Adds diagnostic logging
/// </para>
/// <para>
/// <strong>Slot/Drive Parsing:</strong> Parses drive names in the format "SlotX-DY"
/// (e.g., "Slot6-D1" → Slot 6, Drive 1) to assign proper slot and drive numbers
/// for status tracking.
/// </para>
/// </remarks>
public class DiskIIFactory(IDiskImageFactory imageFactory, IDiskStatusMutator diskStatusMutator) : IDiskIIFactory
{
    private readonly IDiskImageFactory _imageFactory = imageFactory;
    private readonly IDiskStatusMutator _diskStatusMutator = diskStatusMutator;

    /// <summary>
    /// Creates a Disk II drive with no disk inserted.
    /// </summary>
    /// <param name="driveName">Name for the drive (e.g., "Slot6-D1").</param>
    /// <returns>A new drive instance with no disk, wrapped in status and debug decorators.</returns>
    public IDiskIIDrive CreateDrive(string driveName)
    {
        // Parse slot and drive numbers from name
        var (slotNumber, driveNumber) = ParseDriveName(driveName);

        // Create core drive (no disk inserted)
        var coreDrive = new DiskIIDrive(driveName, imageProvider: null, diskImageFactory: _imageFactory);

        // Wrap in status decorator for status tracking
        var statusDrive = new DiskIIStatusDecorator(coreDrive, _diskStatusMutator, slotNumber, driveNumber);

        // Wrap in debug decorator for diagnostic logging (outermost layer)
        return new DiskIIDebugDecorator(statusDrive);
    }

    /// <summary>
    /// Creates a Disk II drive with a disk image loaded.
    /// </summary>
    /// <param name="driveName">Name for the drive (e.g., "Slot6-D1").</param>
    /// <param name="diskImagePath">Path to disk image file (.nib, .woz, .dsk, etc.).</param>
    /// <returns>A new drive instance with the specified disk loaded.</returns>
    public IDiskIIDrive CreateDriveWithDisk(string driveName, string diskImagePath)
    {
        // Parse slot and drive numbers from name
        var (slotNumber, driveNumber) = ParseDriveName(driveName);

        // Create image provider
        IDiskImageProvider provider = _imageFactory.CreateProvider(diskImagePath);

        // Create core drive with disk
        var coreDrive = new DiskIIDrive(driveName, provider, _imageFactory);

        // Wrap in status decorator
        var statusDrive = new DiskIIStatusDecorator(coreDrive, _diskStatusMutator, slotNumber, driveNumber);

        // Wrap in debug decorator (outermost layer)
        return new DiskIIDebugDecorator(statusDrive);
    }

    /// <summary>
    /// Parses drive name in the format "SlotX-DY" to extract slot and drive numbers.
    /// </summary>
    /// <param name="driveName">Drive name (e.g., "Slot6-D1").</param>
    /// <returns>Tuple of (slotNumber, driveNumber), or (6, 1) as default if parsing fails.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Format:</strong> Expects names like "Slot6-D1", "Slot2-D2", etc.
    /// </para>
    /// <para>
    /// <strong>Fallback:</strong> Returns (6, 1) if parsing fails, which corresponds to
    /// the typical boot slot configuration (Slot 6, Drive 1).
    /// </para>
    /// </remarks>
    private static (int slotNumber, int driveNumber) ParseDriveName(string driveName)
    {
        try
        {
            // Expected format: "Slot6-D1"
            // Split on '-' to get ["Slot6", "D1"]
            var parts = driveName.Split('-');
            if (parts.Length != 2)
            {
                return (6, 1); // Default fallback
            }

            // Extract slot number from "Slot6"
            string slotPart = parts[0];
            if (!slotPart.StartsWith("Slot", StringComparison.OrdinalIgnoreCase))
            {
                return (6, 1);
            }
            int slotNumber = int.Parse(slotPart.Substring(4));

            // Extract drive number from "D1"
            string drivePart = parts[1];
            if (!drivePart.StartsWith("D", StringComparison.OrdinalIgnoreCase))
            {
                return (6, 1);
            }
            int driveNumber = int.Parse(drivePart.Substring(1));

            return (slotNumber, driveNumber);
        }
        catch
        {
            // Parsing failed - return default
            return (6, 1);
        }
    }
}

