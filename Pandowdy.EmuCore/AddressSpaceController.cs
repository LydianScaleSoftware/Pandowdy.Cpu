using Emulator;
using Pandowdy.EmuCore.Interfaces;
using Pandowdy.EmuCore.Services;

namespace Pandowdy.EmuCore;

/// <summary>
/// Event arguments for memory access notifications to non-UI consumers.
/// </summary>
/// <remarks>
/// Used to notify observers (debuggers, trace logs, etc.) when memory is read or written.
/// The <see cref="Value"/> is null for read notifications, non-null for write notifications.
/// </remarks>
public sealed class MemoryAccessEventArgs : EventArgs
{
    /// <summary>
    /// Gets the 16-bit address that was accessed ($0000-$FFFF).
    /// </summary>
    public ushort Address { get; init; }
    
    /// <summary>
    /// Gets the byte value that was written, or null if this was a read operation.
    /// </summary>
    public byte? Value { get; init; }
}


public sealed class AddressSpaceController : IMemory, IMemoryAccessNotifier, IDirectMemoryPoolReader,  IDisposable
{
    //Methods from IMemory:
    
    /// <summary>
    /// Gets the size of the addressable memory space (always 64KB for 6502).
    /// </summary>
    /// <value>65,536 bytes ($0000-$FFFF).</value>
    /// <remarks>
    /// The 6502 has a 16-bit address bus, providing 64KB of address space. The actual
    /// physical memory may be larger (128KB main+aux + ROM), but it's accessed through
    /// this 64KB window via soft switch-controlled bank switching.
    /// </remarks>
    public int Size { get => 0x10000;  } // 64k addressable space



    /// <summary>
    /// Gets or sets a byte at the specified address (indexer syntax).
    /// </summary>
    /// <param name="address">16-bit address ($0000-$FFFF).</param>
    /// <returns>Byte value at the mapped physical location.</returns>
    /// <remarks>
    /// Provides array-like syntax for memory access: <c>memory[0x1000] = 0x42;</c>
    /// Delegates to <see cref="Read"/> and <see cref="Write"/>.
    /// </remarks>
    public byte this[ushort address]
    {
        get => Read(address);
        set => Write(address, value);
    }

    //Methods from IMemoryAccessNotifier:

    /// <summary>
    /// Event raised when memory is written to.
    /// </summary>
    /// <remarks>
    /// Consumers (debuggers, trace logs, memory viewers) can subscribe to this event
    /// to monitor memory writes. The event includes the address and value written.
    /// Only fires for successful writes (not write-protected regions).
    /// </remarks>
    public event EventHandler<MemoryAccessEventArgs>? MemoryWritten;

    /// <summary>
    /// Event raised when memory is read from.
    /// </summary>
    /// <remarks>
    /// Currently not implemented (no reads trigger this event). Reserved for future
    /// use by debuggers or profilers that need to track memory access patterns.
    /// </remarks>
#pragma warning disable CS0067 // Event is never used - reserved for future debugger/profiler support
    public event EventHandler<MemoryAccessEventArgs>? MemoryRead;
#pragma warning restore CS0067


    //Methods from IDirectMemoryPoolReader:

    /// <summary>
    /// Reads directly from the main memory bank, bypassing soft switch mapping.
    /// </summary>
    /// <param name="address">Physical address in the pool (0-65535 for main bank).</param>
    /// <returns>Byte value at the physical location.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Direct Access:</strong> This method bypasses the soft switch mapping system
    /// and reads directly from the physical main memory bank. Used by debuggers, memory
    /// viewers, and video renderers that need to see actual RAM contents regardless of
    /// current bank switching.
    /// </para>
    /// <para>
    /// <strong>Example:</strong> The video renderer needs to read from hi-res page 2 even
    /// if PAGE2 is off, so it uses this method instead of the normal <see cref="Read"/> method.
    /// </para>
    /// </remarks>
    public byte ReadRawMain(int address) => _systemRam.ReadRawMain(address);
    
    /// <summary>
    /// Reads directly from the auxiliary memory bank, bypassing soft switch mapping.
    /// </summary>
    /// <param name="address">Physical address in the pool (0-65535 for aux bank).</param>
    /// <returns>Byte value at the physical location.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Direct Access:</strong> Reads from the auxiliary 64KB bank (offset 0x10000
    /// in the pool). Used for 80-column display, double hi-res graphics, and debugging.
    /// </para>
    /// <para>
    /// <strong>Example:</strong> 80-column text mode interleaves main and auxiliary memory
    /// for each character position, so the renderer uses this method to access aux memory
    /// directly.
    /// </para>
    /// </remarks>

    public byte ReadRawAux(int address) => _systemRam.ReadRawAux(address);


    
    // Region slices (instance readonly; previously static)

    private const int RequiredRamSize = 0xC000; // 48KB

    private ISlots _slots;

    private ISystemStatusProvider _status;

    private ILanguageCard _langCard;
    private ISystemRamSelector _systemRam;

    public ISystemRamSelector SystemRam { get => _systemRam; }

    public AddressSpaceController(
        ISystemStatusProvider status,
        ILanguageCard langCard,
        ISystemRamSelector systemRam,
        ISlots slots)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(langCard);
        ArgumentNullException.ThrowIfNull(systemRam);
        ArgumentNullException.ThrowIfNull(slots);

        _slots = slots;
        _status = status;
        _langCard = langCard;
        _systemRam = Utility.ValidateIMemorySize(systemRam, nameof(systemRam), RequiredRamSize);
    }



    public byte Read(ushort address) => address switch
    {
        >= 0xE000 => _langCard.Read(address),        // $E000-$FFFF
        >= 0xD000 => _langCard.Read(address),        // $D000-$DFFF
        >= 0xC090 => _slots.Read((ushort) (address - 0xC000)), // $C090-$CFFF
        >= 0xC000 => throw new InvalidOperationException($"AddressSpaceController should never receive $C000-$C08F (VA2MBus intercepts). Address: ${address:X4}"),
        _ => _systemRam.Read(address)                // $0000-$BFFF
    };

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case >= 0xE000:
            case >= 0xD000:
                _langCard.Write(address, value);
                break;

            case >= 0xC090:
                _slots.Write((ushort) (address - 0xC000), value);
                break;

            case >= 0xC000:
                throw new InvalidOperationException($"AddressSpaceController should never receive $C000-$C08F (VA2MBus intercepts). Address: ${address:X4}");

            default: // $0000-$BFFF
                _systemRam.Write(address, value);
                break;
        }

        MemoryWritten?.Invoke(this, new MemoryAccessEventArgs { Address = address, Value = value });
    }

    // Thread synchronization for memory mapping updates
    private readonly ReaderWriterLockSlim _mappingLock = new(LockRecursionPolicy.NoRecursion);

    public void Dispose()
    {

    }


}
