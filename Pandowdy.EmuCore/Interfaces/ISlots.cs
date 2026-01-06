using Emulator;

namespace Pandowdy.EmuCore.Interfaces;

/// <summary>
/// Defines the slot numbers for Apple IIe expansion cards.
/// </summary>
/// <remarks>
/// The Apple IIe provides seven expansion slots numbered 1 through 7. Slot 0 is
/// reserved for system use and is not accessible to expansion cards. Each slot
/// provides access to three address spaces: I/O ($C0x0-$C0xF), ROM ($Cx00-$CxFF),
/// and shared extended ROM ($C800-$CFFF).
/// </remarks>
public enum SlotNumber
{
    /// <summary>Expansion slot 1.</summary>
    Slot1,
    /// <summary>Expansion slot 2.</summary>
    Slot2,
    /// <summary>Expansion slot 3 (typically 80-column card).</summary>
    Slot3,
    /// <summary>Expansion slot 4.</summary>
    Slot4,
    /// <summary>Expansion slot 5.</summary>
    Slot5,
    /// <summary>Expansion slot 6 (typically disk controller).</summary>
    Slot6,
    /// <summary>Expansion slot 7.</summary>
    Slot7
}

/// <summary>
/// Manages Apple IIe expansion slots and coordinates peripheral card I/O and ROM access.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ISlots"/> interface coordinates all expansion card operations in the
/// Apple IIe emulator, managing the complex address decoding logic that determines which
/// device (card or system ROM) responds to memory accesses in the $C000-$CFFF range.
/// </para>
/// <para>
/// <strong>Address Space Management:</strong>
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Address Range</term>
/// <description>Purpose</description>
/// </listheader>
/// <item>
/// <term>$C090-$C0FF</term>
/// <description>Card I/O space (16 bytes per slot: $C0n0-$C0nF where n = slot + 8)</description>
/// </item>
/// <item>
/// <term>$C100-$C7FF</term>
/// <description>Card ROM space (256 bytes per slot: $Cn00-$CnFF where n = slot)</description>
/// </item>
/// <item>
/// <term>$C800-$CFFF</term>
/// <description>Extended ROM space (2KB shared via bank switching)</description>
/// </item>
/// </list>
/// <para>
/// <strong>Soft Switch Control:</strong><br/>
/// The slots system respects three soft switches that control ROM visibility:
/// </para>
/// <list type="bullet">
/// <item><description><strong>INTCXROM</strong> ($C006/$C007): Master override—when enabled, internal ROM appears for entire $C100-$CFFF range, disabling all cards</description></item>
/// <item><description><strong>SLOTCXROM</strong> ($C00A/$C00B): When INTCXROM is off, controls whether slots 1,2,4-7 use card ROM or system ROM</description></item>
/// <item><description><strong>SLOTC3ROM</strong> ($C00C/$C00D): Independent control for slot 3 (typically 80-column firmware)</description></item>
/// </list>
/// <para>
/// <strong>Extended ROM Bank Switching:</strong><br/>
/// The $C800-$CFFF region is shared among all slots via the <see cref="BankSelect"/> mechanism:
/// </para>
/// <list type="bullet">
/// <item><description>Accessing a slot's ROM ($Cn00-$CnFF) activates that slot's extended ROM at $C800-$CFFF</description></item>
/// <item><description>Accessing $CFFF from any slot disables extended ROM (sets <see cref="BankSelect"/> to 0)</description></item>
/// <item><description>Only one slot's extended ROM can be active at a time</description></item>
/// </list>
/// <para>
/// <strong>Null Card Behavior:</strong><br/>
/// Empty slots are filled with <see cref="ICardFactory.GetNullCard"/> instances that return
/// <c>null</c> for all operations, causing the slots system to return floating bus values
/// for reads and no-op for writes.
/// </para>
/// <para>
/// <strong>Configuration Management:</strong><br/>
/// The slots system implements <see cref="IConfigurable"/>, enabling complete save/restore
/// of the entire peripheral configuration. The metadata includes which cards are installed
/// in which slots, along with each card's configuration (hierarchically embedded). This
/// allows the entire emulator peripheral setup to be persisted, shared, and restored,
/// including disk images, serial settings, and other card-specific state. The metadata
/// format is typically JSON. See <see cref="IConfigurable"/> for the configuration protocol.
/// </para>
/// </remarks>
/// <seealso cref="IMemory"/>
/// <seealso cref="IConfigurable"/>
/// <seealso cref="ICard"/>
/// <seealso cref="ICardFactory"/>
public interface ISlots : IMemory, IConfigurable
{
    /// <summary>
    /// Gets or sets the currently active slot for extended ROM at $C800-$CFFF.
    /// </summary>
    /// <value>
    /// A value from 0-7 where:
    /// <list type="bullet">
    /// <item><description>0 = No slot active (extended ROM disabled)</description></item>
    /// <item><description>1-7 = That slot's extended ROM is active at $C800-$CFFF</description></item>
    /// </list>
    /// </value>
    /// <remarks>
    /// <para>
    /// This property implements the Apple IIe's extended ROM bank switching mechanism.
    /// The hardware allows only one slot's extended ROM to be visible at $C800-$CFFF
    /// at any given time. The active slot is determined by:
    /// </para>
    /// <list type="number">
    /// <item><description>Reading/writing $Cn00-$CnFF sets <see cref="BankSelect"/> to slot n</description></item>
    /// <item><description>Reading/writing $CFFF sets <see cref="BankSelect"/> to 0 (disabled)</description></item>
    /// </list>
    /// <para>
    /// The bank selection state is preserved even when INTCXROM is enabled (which overrides
    /// extended ROM with system ROM but doesn't clear the selection). This allows the
    /// previously selected card's extended ROM to reappear when INTCXROM is disabled.
    /// </para>
    /// <para>
    /// <strong>Example:</strong> A Disk II controller in slot 6 has boot ROM at $C600-$C6FF
    /// and extended driver code at its extended ROM. When the system reads $C600, slot 6's
    /// extended ROM becomes active at $C800-$CFFF. Reading $CFFF disables extended ROM.
    /// </para>
    /// </remarks>
    public byte BankSelect { get; set; }

    /// <summary>
    /// Installs a peripheral card in the specified slot using its numeric card ID.
    /// </summary>
    /// <param name="id">The unique numeric identifier of the card type to install (see <see cref="ICard.Id"/>).</param>
    /// <param name="slot">The slot number (1-7) where the card should be installed.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the card factory cannot create a card with the specified ID.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses the <see cref="ICardFactory"/> to create a new instance of the
    /// specified card type via <see cref="ICardFactory.GetCardWithId"/>. The card is
    /// cloned from a registered prototype, ensuring each slot gets an independent instance.
    /// </para>
    /// <para>
    /// If a card is already installed in the specified slot, it is replaced by the new card.
    /// No state from the previous card is preserved.
    /// </para>
    /// <para>
    /// Common card IDs:
    /// <list type="bullet">
    /// <item><description>0 = NullCard (empty slot)</description></item>
    /// <item><description>1 = Disk II Controller</description></item>
    /// <item><description>2 = Super Serial Card</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <seealso cref="InstallCard(string, SlotNumber)"/>
    /// <seealso cref="RemoveCard"/>
    public void InstallCard(int id, SlotNumber slot);

    /// <summary>
    /// Installs a peripheral card in the specified slot using its human-readable name.
    /// </summary>
    /// <param name="name">The name of the card type to install (see <see cref="ICard.Name"/>).</param>
    /// <param name="slot">The slot number (1-7) where the card should be installed.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the card factory cannot create a card with the specified name.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method provides a more user-friendly alternative to <see cref="InstallCard(int, SlotNumber)"/>
    /// by accepting the card's display name instead of its numeric ID. The card name must exactly
    /// match the value returned by <see cref="ICard.Name"/> (case-sensitive).
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// slots.InstallCard("Disk II Controller", SlotNumber.Slot6);
    /// slots.InstallCard("Super Serial Card", SlotNumber.Slot2);
    /// </code>
    /// </para>
    /// <para>
    /// If a card is already installed in the specified slot, it is replaced by the new card.
    /// </para>
    /// </remarks>
    /// <seealso cref="InstallCard(int, SlotNumber)"/>
    /// <seealso cref="RemoveCard"/>
    public void InstallCard(string name, SlotNumber slot);

    /// <summary>
    /// Removes the card from the specified slot, replacing it with an empty slot (NullCard).
    /// </summary>
    /// <param name="slot">The slot number (1-7) to clear.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the card factory cannot create a NullCard replacement.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method replaces the card in the specified slot with a <see cref="ICardFactory.GetNullCard"/>
    /// instance. The NullCard returns <c>null</c> for all read operations and no-ops for all writes,
    /// causing the slot to behave as if it's empty (returning floating bus values for reads).
    /// </para>
    /// <para>
    /// Any state maintained by the removed card is lost. If the removed card was the active
    /// extended ROM slot (see <see cref="BankSelect"/>), the extended ROM remains active
    /// but will return floating bus values since NullCard returns <c>null</c>.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This method cannot fail due to the card being "in use" since
    /// the emulator doesn't maintain references to installed cards outside the slots array.
    /// </para>
    /// </remarks>
    /// <seealso cref="InstallCard(int, SlotNumber)"/>
    /// <seealso cref="InstallCard(string, SlotNumber)"/>
    public void RemoveCard(SlotNumber slot);

    /// <summary>
    /// Retrieves the card currently installed in the specified slot.
    /// </summary>
    /// <param name="slot">The slot number (1-7) to query.</param>
    /// <returns>
    /// The <see cref="ICard"/> instance installed in the slot. Returns a <see cref="ICardFactory.GetNullCard"/>
    /// instance if the slot is empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides direct access to the card instance for inspection, debugging,
    /// or advanced operations. Normal emulator operation doesn't require calling this method
    /// since <see cref="IMemory.Read"/> and <see cref="IMemory.Write"/> automatically route
    /// to the appropriate card based on address.
    /// </para>
    /// <para>
    /// <strong>Usage Examples:</strong>
    /// <code>
    /// // Check what's installed in slot 6
    /// ICard card = slots.GetCardIn(SlotNumber.Slot6);
    /// Console.WriteLine($"Slot 6: {card.Name}");
    /// 
    /// // Verify a slot is empty
    /// if (slots.GetCardIn(SlotNumber.Slot3).Id == 0) // NullCard.Id == 0
    /// {
    ///     Console.WriteLine("Slot 3 is empty");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ICard GetCardIn(SlotNumber slot);

    /// <summary>
    /// Determines whether the specified slot is empty (contains a NullCard).
    /// </summary>
    /// <param name="slot">The slot number (1-7) to check.</param>
    /// <returns>
    /// <c>true</c> if the slot contains a <see cref="ICardFactory.GetNullCard"/> (empty slot);
    /// <c>false</c> if the slot contains an actual peripheral card.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method that checks whether the card in the specified slot
    /// has an <see cref="ICard.Id"/> of 0 (the NullCard identifier). It provides a more
    /// readable alternative to manually checking the card ID.
    /// </para>
    /// <para>
    /// <strong>Usage Examples:</strong>
    /// <code>
    /// // Clear alternative to checking card ID
    /// if (slots.IsEmpty(SlotNumber.Slot6))
    /// {
    ///     slots.InstallCard("Disk II Controller", SlotNumber.Slot6);
    /// }
    /// 
    /// // List all installed cards
    /// for (int i = 1; i &lt;= 7; i++)
    /// {
    ///     var slot = (SlotNumber)(i - 1);
    ///     if (!slots.IsEmpty(slot))
    ///     {
    ///         Console.WriteLine($"Slot {i}: {slots.GetCardIn(slot).Name}");
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// This method is equivalent to:
    /// <code>
    /// bool IsEmpty(SlotNumber slot) => GetCardIn(slot).Id == 0;
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="GetCardIn"/>
    /// <seealso cref="RemoveCard"/>
    public bool IsEmpty(SlotNumber slot);

    //** Inherited from IMemory **
    //
    // The following members are inherited from IMemory and handle the $C090-$CFFF address range:
    //
    // byte Read(ushort address)
    //   Reads a byte from the slots address space ($C090-$CFFF, offset by $C000).
    //   Handles card I/O, card ROM, and extended ROM based on soft switch settings.
    //   Returns floating bus values for empty slots or when cards return null.
    //
    // void Write(ushort address, byte value)
    //   Writes a byte to the slots address space ($C090-$CFFF, offset by $C000).
    //   Handles card I/O, card ROM, and extended ROM based on soft switch settings.
    //   Most writes are no-ops since ROM is read-only, but some cards may use RAM.
    //
    // byte this[ushort address] { get; set; }
    //   Indexer providing array-style access to Read/Write methods.
    //
    // Note: All addresses are offset by $C000. For example, to access $C600, pass 0x0600.
}

