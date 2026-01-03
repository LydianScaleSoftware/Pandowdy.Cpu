using Emulator;

namespace Pandowdy.EmuCore.Interfaces;

/// <summary>
/// Apple II specific bus interface that extends the base IBus with
/// keyboard input and pushbutton (game controller) functionality.
/// </summary>
/// <remarks>
/// This interface represents the Apple IIe system bus, coordinating communication
/// between the CPU, memory, and I/O devices. It handles keyboard input, game controller
/// buttons, and provides access to the system clock counter for timing-sensitive operations.
/// </remarks>
#pragma warning disable CS0108 // Member hides inherited member; IBus will be removed soon
public interface IAppleIIBus : IBus
{
    /// <summary>
    /// Sets the keyboard latch value (typically with high bit set).
    /// </summary>
    /// <param name="key">The key value to store in the keyboard latch. 
    /// The high bit (bit 7) should typically be set to indicate a key is available.</param>
    /// <remarks>
    /// On the Apple II, reading address $C000 returns the keyboard latch value,
    /// and reading $C010 clears the high bit. This method simulates external keyboard
    /// input by setting the latch value that the CPU will read.
    /// </remarks>
    void SetKeyValue(byte key);

    /// <summary>
    /// Sets the state of a pushbutton (game controller button).
    /// </summary>
    /// <param name="num">Button number (0-2 for buttons 0, 1, and 2)</param>
    /// <param name="enabled">True if the button is pressed, false if released</param>
    /// <remarks>
    /// Apple II game controllers have up to 3 buttons that can be read at addresses
    /// $C061 (button 0), $C062 (button 1), and $C063 (button 2). The high bit of
    /// the returned byte indicates button state (1=pressed, 0=released).
    /// </remarks>
    void SetPushButton(int num, bool enabled);

    /// <summary>
    /// Gets the memory pool representing the Apple IIe's 64k addressable memory space.
    /// </summary>
    /// <value>
    /// The memory instance that provides read/write access to main RAM, auxiliary RAM,
    /// ROM, and language card memory banks.
    /// </value>
    IMemory RAM { get; }

    /// <summary>
    /// Gets the 6502 CPU instance connected to this bus.
    /// </summary>
    /// <value>
    /// The CPU that executes instructions and communicates with memory and I/O
    /// devices through this bus interface.
    /// </value>
    ICpu Cpu { get; }

    /// <summary>
    /// Gets the system clock counter tracking elapsed cycles since reset.
    /// </summary>
    /// <value>
    /// The total number of clock cycles elapsed. This counter increments with each
    /// call to <see cref="Clock"/> and is used for timing VBlank events and other
    /// time-sensitive operations. Runs at approximately 1.023 MHz.
    /// </value>
    UInt64 SystemClockCounter { get; }

    /// <summary>
    /// Reads a byte from the specified memory address as the CPU would.
    /// </summary>
    /// <param name="address">The 16-bit memory address to read from ($0000-$FFFF)</param>
    /// <param name="readOnly">If true, performs a read without side effects (for debugging).
    /// If false, may trigger side effects like clearing soft switches or strobing addresses.</param>
    /// <returns>The byte value at the specified address</returns>
    /// <remarks>
    /// This method handles I/O address decoding for the $C000-$CFFF range, routing
    /// reads to appropriate handlers for keyboard, soft switches, and peripheral cards.
    /// Regular memory reads ($0000-$BFFF, $D000-$FFFF) are routed to the memory pool
    /// with bank switching and auxiliary memory selection applied.
    /// </remarks>
    Byte CpuRead(UInt16 address, bool readOnly = false);
    
    /// <summary>
    /// Writes a byte to the specified memory address as the CPU would.
    /// </summary>
    /// <param name="address">The 16-bit memory address to write to ($0000-$FFFF)</param>
    /// <param name="data">The byte value to write</param>
    /// <remarks>
    /// This method handles I/O address decoding for the $C000-$CFFF range, routing
    /// writes to appropriate handlers for soft switches, language card banking,
    /// and peripheral cards. Regular memory writes are routed to the memory pool
    /// with bank switching, write protection, and auxiliary memory selection applied.
    /// Many Apple II soft switches are write-triggered, changing system state based
    /// on the write address rather than the data value.
    /// </remarks>
    void CpuWrite(UInt16 address, byte data);
    
    /// <summary>
    /// Advances the system clock by one cycle.
    /// </summary>
    /// <remarks>
    /// Increments the <see cref="SystemClockCounter"/> and checks for VBlank timing.
    /// Should be called once per CPU instruction cycle. The Apple IIe runs at
    /// approximately 1.023 MHz, so this method is typically called about 1 million
    /// times per second during emulation.
    /// </remarks>
    void Clock();

    /// <summary>
    /// Resets the bus and connected devices to their initial power-on state.
    /// </summary>
    /// <remarks>
    /// Resets the system clock counter to zero, clears the keyboard latch,
    /// resets soft switches to their default values, and triggers a CPU reset.
    /// This simulates powering on the Apple IIe or pressing the reset button.
    /// </remarks>
    void Reset();
}
#pragma warning restore CS0108

