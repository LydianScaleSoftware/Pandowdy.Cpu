namespace Pandowdy.EmuCore.Interfaces;

/// <summary>
/// Interface for reading keyboard input with Apple IIe hardware-accurate strobe behavior.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Apple IIe Keyboard Mechanism:</strong> The Apple IIe keyboard uses a strobe bit (bit 7)
/// to indicate when a new key has been pressed but not yet read by software. When a key is pressed,
/// the 7-bit ASCII code (0-127) is placed in bits 0-6, and bit 7 is set to 1 (creating a value >= 128).
/// </para>
/// <para>
/// <strong>Hardware Addresses:</strong>
/// <list type="bullet">
/// <item>$C000 (KBD) - Read keyboard latch with strobe (returns value with bit 7 set if unread)</item>
/// <item>$C010 (KBDSTRB) - Clear keyboard strobe (reading this address clears bit 7)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Strobe Lifecycle:</strong>
/// <list type="number">
/// <item>Key pressed → Strobe set (bit 7 = 1), value = ASCII + 128</item>
/// <item>Program reads $C000 → Returns key with strobe (e.g., 0xC1 for 'A')</item>
/// <item>Program reads $C010 → Strobe cleared (bit 7 = 0), subsequent reads return ASCII only (e.g., 0x41)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Implementation Note:</strong> This interface supports both single-key and buffered keyboard
/// implementations. The <see cref="SingularKeyHandler"/> provides a simple single-key implementation
/// matching original Apple IIe behavior (new keypress overwrites previous unread key).
/// </para>
/// </remarks>
public interface IKeyboardReader
{
    /// <summary>
    /// Enqueues a raw key value with strobe bit automatically set.
    /// </summary>
    /// <param name="key">The 7-bit ASCII character code (0-127). Bit 7 will be set automatically.</param>
    /// <remarks>
    /// <para>
    /// This method simulates a physical key press on the Apple IIe keyboard. The strobe bit (bit 7)
    /// is automatically set to indicate the key is unread, regardless of the input value's bit 7 state.
    /// </para>
    /// <para>
    /// <strong>Behavior:</strong>
    /// <list type="bullet">
    /// <item><strong>Single-key systems</strong> (e.g., <see cref="SingularKeyHandler"/>) - Replaces the current key</item>
    /// <item><strong>Buffered systems</strong> - Adds key to queue (FIFO behavior)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example:</strong> Calling <c>EnqueueKey(0x41)</c> (ASCII 'A') stores 0xC1 internally (0x41 | 0x80).
    /// </para>
    /// </remarks>
    public void EnqueueKey(byte key);

    /// <summary>
    /// Checks if there is an unread key with strobe bit set.
    /// </summary>
    /// <returns>
    /// <c>true</c> if a key is pending (strobe bit 7 is set); <c>false</c> if no unread key or strobe was cleared.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method checks the internal keyboard latch for the strobe bit without modifying the state.
    /// It corresponds to testing bit 7 of the value that would be read from $C000 (KBD).
    /// </para>
    /// <para>
    /// <strong>Apple IIe Usage Pattern:</strong>
    /// <code>
    /// ; Assembly code polling for keypress
    /// WAIT_KEY:
    ///     LDA $C000      ; Read keyboard
    ///     BPL WAIT_KEY   ; Loop if bit 7 clear (no key)
    ///     STA $C010      ; Clear strobe
    ///     AND #$7F       ; Mask off strobe bit
    /// </code>
    /// </para>
    /// </remarks>
    public bool StrobePending();

    /// <summary>
    /// Returns the 7-bit ASCII character code of the current key without the strobe bit.
    /// </summary>
    /// <returns>The ASCII character code (0-127) with bit 7 cleared, regardless of strobe state.</returns>
    /// <remarks>
    /// <para>
    /// This method returns the lower 7 bits of the current key value, equivalent to reading $C000
    /// and masking with 0x7F (AND #$7F). The strobe bit is always cleared in the returned value.
    /// </para>
    /// <para>
    /// <strong>Use Case:</strong> Useful for inspecting the key value without clearing the strobe,
    /// or for reading the key after the strobe has already been cleared via <see cref="FetchPendingAndClearStrobe"/>.
    /// </para>
    /// <para>
    /// <strong>Example:</strong> If the internal value is 0xC1 (strobe set, 'A' key), this returns 0x41.
    /// </para>
    /// </remarks>
    public byte PeekCurrentKeyValue();

    /// <summary>
    /// Returns the raw keyboard latch value including the strobe bit (bit 7).
    /// </summary>
    /// <returns>The full 8-bit keyboard value with strobe bit intact (0-255).</returns>
    /// <remarks>
    /// <para>
    /// This method simulates reading from $C000 (KBD) on the Apple IIe. The returned value includes
    /// bit 7 (the strobe bit), which indicates whether the key has been read:
    /// <list type="bullet">
    /// <item>Bit 7 = 1 (value >= 128): Key is unread, strobe active</item>
    /// <item>Bit 7 = 0 (value &lt; 128): Key has been read, strobe cleared</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Important:</strong> This method does <em>not</em> clear the strobe. To clear the strobe,
    /// call <see cref="FetchPendingAndClearStrobe"/> or simulate reading $C010 (KBDSTRB).
    /// </para>
    /// <para>
    /// <strong>Example Values:</strong>
    /// <list type="bullet">
    /// <item>0xC1 = 'A' key pressed, unread (0x41 | 0x80)</item>
    /// <item>0x41 = 'A' key, strobe cleared</item>
    /// <item>0x8D = Return key pressed, unread (0x0D | 0x80)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public byte PeekCurrentKeyAndStrobe();

    /// <summary>
    /// Returns the next pending key and clears its strobe bit, or <c>null</c> if no unread key is pending.
    /// </summary>
    /// <returns>
    /// The 7-bit ASCII character code (0-127) of the pending key with strobe cleared,
    /// or <c>null</c> if no key is pending (strobe already cleared).
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method simulates the sequence of reading $C000 (KBD) followed by reading $C010 (KBDSTRB)
    /// to clear the keyboard strobe. It atomically checks for a pending key and clears the strobe bit.
    /// </para>
    /// <para>
    /// <strong>Single-Key Behavior:</strong> Returns the current key with strobe cleared, or <c>null</c>
    /// if the strobe was already cleared. The internal state is updated to clear bit 7.
    /// </para>
    /// <para>
    /// <strong>Buffered Keyboard Behavior:</strong> Dequeues the next key from the buffer (FIFO),
    /// returns it with strobe cleared, and removes it from the queue. Returns <c>null</c> if buffer is empty.
    /// </para>
    /// <para>
    /// <strong>Return Value Note:</strong> The returned byte has bit 7 cleared (0-127 range) even though
    /// the key was pending with strobe set. Use <see cref="PeekCurrentKeyAndStrobe"/> if you need to
    /// inspect the strobe state before clearing it.
    /// </para>
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <code>
    /// // C# emulator code
    /// if (keyboardReader.StrobePending())
    /// {
    ///     byte? key = keyboardReader.FetchPendingAndClearStrobe();
    ///     if (key.HasValue)
    ///     {
    ///         ProcessKey(key.Value);  // Value is 0x41 for 'A', not 0xC1
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public byte? FetchPendingAndClearStrobe();
}
