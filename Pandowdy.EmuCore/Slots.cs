using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore;

/// <summary>
/// Implements the Apple IIe expansion slot system, managing peripheral cards and their I/O and ROM access.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Slots"/> class is the concrete implementation of <see cref="ISlots"/>, providing
/// the complete address decoding logic for the Apple IIe's $C000-$CFFF peripheral I/O range.
/// It coordinates between peripheral cards, system ROM, and soft switch settings to determine
/// which device responds to memory accesses.
/// </para>
/// <para>
/// <strong>Architecture:</strong><br/>
/// The slots system maintains an array of 8 card positions (0-7), where slot 0 is reserved
/// for system use and slots 1-7 are available for peripheral cards. Empty slots are filled
/// with <see cref="NullCard"/> instances that return <c>null</c> for all operations, causing
/// floating bus behavior.
/// </para>
/// <para>
/// <strong>Address Decoding Priority:</strong>
/// </para>
/// <list type="number">
/// <item><description><strong>INTCXROM check</strong> - If enabled, system ROM overrides all cards</description></item>
/// <item><description><strong>Address range decode</strong> - Determines I/O, ROM, or Extended ROM space</description></item>
/// <item><description><strong>SLOTCXROM/SLOTC3ROM check</strong> - Determines card vs. system ROM (when INTCXROM off)</description></item>
/// <item><description><strong>Card query</strong> - Asks the card for data (may return null)</description></item>
/// <item><description><strong>Fallback</strong> - Returns system ROM or floating bus value</description></item>
/// </list>
/// <para>
/// <strong>Extended ROM Bank Selection:</strong><br/>
/// The <see cref="BankSelect"/> property tracks which slot (1-7) currently owns the $C800-$CFFF
/// extended ROM space. Accessing a slot's ROM ($Cn00-$CnFF) activates that slot; accessing
/// $CFFF disables all extended ROM.
/// </para>
/// <para>
/// <strong>Configuration Management:</strong><br/>
/// The <see cref="Slots"/> class implements <see cref="IConfigurable"/> (via <see cref="ISlots"/>),
/// enabling complete save/restore of the slot configuration. The metadata format includes:
/// </para>
/// <list type="bullet">
/// <item><description>Which cards are installed in which slots (by ID)</description></item>
/// <item><description>Each card's configuration metadata (hierarchical inclusion)</description></item>
/// <item><description>Current <see cref="BankSelect"/> state (optional)</description></item>
/// </list>
/// <para>
/// This allows the entire peripheral configuration to be saved, shared, and restored,
/// including disk images, serial port settings, and other card-specific configurations.
/// Empty slots are omitted from the metadata to keep it concise.
/// </para>
/// <para>
/// <strong>Threading:</strong><br/>
/// This class is not thread-safe. All methods must be called from the emulator worker thread.
/// Card installation/removal during emulation may cause race conditions.
/// </para>
/// </remarks>
public class Slots : ISlots
{
    /// <summary>
    /// Cached JsonSerializerOptions for configuration serialization to avoid allocation overhead.
    /// </summary>
    private static readonly System.Text.Json.JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true
    };


    /// <summary>
    /// Array of installed cards indexed by slot number (0-7).
    /// </summary>
    /// <remarks>
    /// Slot 0 is reserved for system use. Slots 1-7 correspond to the physical expansion slots.
    /// Empty slots contain <see cref="NullCard"/> instances.
    /// </remarks>
    private ICard[] _cards;

    /// <summary>
    /// Factory for creating card instances.
    /// </summary>
    private ICardFactory _factory;
    
    /// <summary>
    /// Provider for system ROM data at $C000-$CFFF.
    /// </summary>
    private ISystemRomProvider _rom;
    
    /// <summary>
    /// Provider for floating bus values when no device responds.
    /// </summary>
    private IFloatingBusProvider _floatingBus;
    
    /// <summary>
    /// Provider for soft switch states (INTCXROM, SLOTCXROM, SLOTC3ROM, etc.).
    /// </summary>
    private ISystemStatusProvider _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="Slots"/> class with all slots empty.
    /// </summary>
    /// <param name="factory">The card factory for creating card instances.</param>
    /// <param name="rom">The system ROM provider for $C000-$CFFF range.</param>
    /// <param name="floatingBus">The floating bus provider for unresponsive addresses.</param>
    /// <param name="status">The system status provider for soft switch states.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any parameter is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the card factory cannot create a NullCard for initialization.
    /// </exception>
    /// <remarks>
    /// <para>
    /// All seven expansion slots (1-7) are initialized with <see cref="NullCard"/> instances,
    /// representing empty slots. Slot 0 is also filled with a NullCard since it's reserved
    /// for system use and should never be accessed.
    /// </para>
    /// <para>
    /// The constructor creates independent NullCard instances via <see cref="ICard.Clone"/>
    /// to ensure each slot has its own instance, even though NullCards are stateless.
    /// </para>
    /// </remarks>
    public Slots(ICardFactory factory, ISystemRomProvider rom, IFloatingBusProvider floatingBus, ISystemStatusProvider status)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(rom);
        ArgumentNullException.ThrowIfNull(floatingBus);
        ArgumentNullException.ThrowIfNull(status);

        _factory = factory;
        _rom = rom;
        _floatingBus = floatingBus;
        _status = status;

        ICard nullcard = _factory.GetNullCard() ?? throw new InvalidOperationException("Could not create a Null Card");
        _cards = [
            nullcard, // Slot 0 is reserved for system
            nullcard.Clone(),
            nullcard.Clone(),
            nullcard.Clone(),
            nullcard.Clone(),
            nullcard.Clone(),
            nullcard.Clone(),
            nullcard.Clone()
         ];
    }

    /// <summary>
    /// Gets the size of the address space managed by this slots system.
    /// </summary>
    /// <value>
    /// Always returns 0x1000 (4096 bytes), representing the $C000-$CFFF range.
    /// </value>
    /// <remarks>
    /// <para>
    /// Although the value indicates $C000-$CFFF (4KB), the slots system only handles
    /// $C090-$CFFF (3952 bytes). The range $C000-$C08F contains soft switches and other
    /// system I/O that's handled elsewhere (typically by <see cref="VA2MBus"/>).
    /// </para>
    /// <para>
    /// If <see cref="Read"/> or <see cref="Write"/> is called with an address in the
    /// $C000-$C08F range (0x0000-0x008F when offset), an <see cref="InvalidOperationException"/>
    /// will be thrown.
    /// </para>
    /// </remarks>
    public int Size { get => 0x1000; }

    /// <inheritdoc/>
    public byte BankSelect { get; set; } = 0;

    /// <inheritdoc/>
    public void InstallCard(int id, SlotNumber slot)
    {
        _cards[(int) slot] = _factory.GetCardWithId(id) ?? throw new InvalidOperationException($"Could not create a card with id {id} for slot {((int) slot) + 1}");
    }
    
    /// <inheritdoc/>
    public void InstallCard(string name, SlotNumber slot)
    {
        _cards[(int) slot] = _factory.GetCardWithName(name) ?? throw new InvalidOperationException($"Could not create a card with name {name} for slot {((int) slot) + 1}");
    }

    /// <inheritdoc/>
    public void RemoveCard(SlotNumber slot)
    {
        _cards[(int) slot] = _factory.GetNullCard() ?? throw new InvalidOperationException($"Could not create a Null Card while removing a card in slot {((int) slot) + 1}");
    }

    /// <inheritdoc/>
    public ICard GetCardIn(SlotNumber slot)
    {
        return _cards[(int) slot];
    }

    /// <inheritdoc/>
    public bool IsEmpty(SlotNumber slot)
    {
        return GetCardIn(slot).Id == 0;
    }

    /// <summary>
    /// Reads a byte from the slots address space ($C090-$CFFF).
    /// </summary>
    /// <param name="address">
    /// The address to read, offset by $C000. For example, to read from $C600, pass 0x0600.
    /// Valid range: 0x0090-0x0FFF.
    /// </param>
    /// <returns>
    /// The byte value from the responding device (card, system ROM, or floating bus).
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the address is outside the valid range ($C090-$CFFF / 0x0090-0x0FFF).
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Address Decoding Logic:</strong>
    /// </para>
    /// <para>
    /// <strong>$C090-$C0FF (Card I/O Space):</strong><br/>
    /// If INTCXROM is enabled, returns system ROM. Otherwise, extracts slot number from
    /// address bits 4-6 and calls the card's <see cref="ICard.ReadIO"/>. Returns floating
    /// bus if card returns <c>null</c>. This range never modifies <see cref="BankSelect"/>.
    /// </para>
    /// <para>
    /// <strong>$C100-$C7FF (Card ROM Space):</strong><br/>
    /// If INTCXROM is enabled, returns system ROM. Otherwise, checks SLOTCXROM (or SLOTC3ROM
    /// for slot 3) to determine card vs. system ROM. If using card ROM, sets <see cref="BankSelect"/>
    /// to the slot number and calls <see cref="ICard.ReadRom"/>. Returns floating bus if
    /// card returns <c>null</c>, or system ROM if soft switches indicate system ROM.
    /// </para>
    /// <para>
    /// <strong>$C800-$CFFF (Extended ROM Space):</strong><br/>
    /// If INTCXROM is enabled, returns system ROM. Otherwise, checks <see cref="BankSelect"/>
    /// to determine which card's extended ROM is active. If <see cref="BankSelect"/> is non-zero,
    /// calls that card's <see cref="ICard.ReadExtendedRom"/>. Returns floating bus if
    /// <see cref="BankSelect"/> is 0 or if card returns <c>null</c>.
    /// </para>
    /// <para>
    /// <strong>Special Behavior at $CFFF:</strong><br/>
    /// Reading from $CFFF (0x0FFF) disables extended ROM by setting <see cref="BankSelect"/>
    /// to 0. The read still returns the byte from the currently active card (if any) before
    /// being disabled.
    /// </para>
    /// </remarks>
    /// <seealso cref="Write"/>
    /// <seealso cref="BankSelect"/>
    public byte Read(ushort address)
    {
        // $CFFF disables extended ROM (both read and write)
        if (address == 0x0FFF)
        {
            BankSelect = 0;
        }

        // $C090-$C0FF: Card I/O space
        if (address >= 0x0090 && address <= 0x00FF)
        {
            // INTCXROM overrides all card I/O and ROM
            if (_status.StateIntCxRom)
            {
                // Internal ROM enabled, return system ROM
                return _rom.Read(address);  // _rom is also offset by $C000
            }

            // Determine slot from address: $C090-$C09F=slot1, $C0A0-$C0AF=slot2, etc.
            int slot = ((address >> 4) & 0x07);
            byte offset = (byte) (address & 0x0F);

            // Card I/O never changes BankSelect
            byte? cardByte = _cards[slot].ReadIO(offset);
            return cardByte ?? _floatingBus.Read();
        }

        // $C100-$C7FF: Card ROM or System ROM
        if (address >= 0x0100 && address <= 0x07FF)
        {
            // INTCXROM overrides SLOTCXROM and SLOTC3ROM
            if (_status.StateIntCxRom)
            {
                // Internal ROM enabled for entire $C100-$CFFF range
                return _rom.Read(address);
            }

            int slot = (address >> 8) & 0x07;
            byte offset = (byte) (address & 0xFF);

            // Determine if this slot should use card ROM or system ROM
            bool useCardRom = !_status.StateIntCxRom;

            // Special case: Slot 3 is controlled by SLOTC3ROM
            if (slot == 3)
            {
                useCardRom = _status.StateSlotC3Rom;
            }

            if (useCardRom)
            {
                // Card ROM enabled: ACTIVATE extended ROM and return card data
                BankSelect = (byte) slot;

                byte? cardByte = _cards[slot].ReadRom(offset);
                return cardByte ?? _floatingBus.Read();
            }
            else
            {
                // System ROM enabled: DON'T change BankSelect
                return _rom.Read(address);
            }
        }

        // $C800-$CFFF: Extended ROM (uses BankSelect regardless of SLOTCXROM/SLOTC3ROM)
        if (address >= 0x0800 && address <= 0x0FFF)
        {
            // INTCXROM overrides extended ROM
            if (_status.StateIntCxRom)
            {
                return _rom.Read(address);
            }

            ushort offset = (ushort) (address - 0x0800);

            if (BankSelect != 0)
            {
                byte? cardByte = _cards[BankSelect].ReadExtendedRom(offset);
                return cardByte ?? _floatingBus.Read();
            }

            return _floatingBus.Read();
        }

        // Shouldn't reach here in normal operation ($C000-$C08F handled elsewhere)
        throw new InvalidOperationException($"ISlots.Read() called with invalid address: ${address + 0xC000:X4}");
    }

    /// <summary>
    /// Writes a byte to the slots address space ($C090-$CFFF).
    /// </summary>
    /// <param name="address">
    /// The address to write, offset by $C000. For example, to write to $C600, pass 0x0600.
    /// Valid range: 0x0090-0x0FFF.
    /// </param>
    /// <param name="val">The byte value to write.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the address is outside the valid range ($C090-$CFFF / 0x0090-0x0FFF).
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Address Decoding Logic:</strong>
    /// </para>
    /// <para>
    /// <strong>$C090-$C0FF (Card I/O Space):</strong><br/>
    /// If INTCXROM is enabled, writes to system ROM (usually a no-op). Otherwise, extracts
    /// slot number from address bits 4-6 and calls the card's <see cref="ICard.WriteIO"/>.
    /// Cards typically use this space for control registers, command triggers, and data output.
    /// This range never modifies <see cref="BankSelect"/>.
    /// </para>
    /// <para>
    /// <strong>$C100-$C7FF (Card ROM Space):</strong><br/>
    /// If INTCXROM is enabled, writes to system ROM (usually a no-op). Otherwise, checks
    /// SLOTCXROM (or SLOTC3ROM for slot 3) to determine card vs. system ROM. If using card
    /// ROM, sets <see cref="BankSelect"/> to the slot number and calls <see cref="ICard.WriteRom"/>.
    /// Most cards treat this as a no-op since ROM is read-only, but some cards may implement
    /// writeable RAM in this space.
    /// </para>
    /// <para>
    /// <strong>$C800-$CFFF (Extended ROM Space):</strong><br/>
    /// If INTCXROM is enabled, writes to system ROM (usually a no-op). Otherwise, if
    /// <see cref="BankSelect"/> is non-zero, calls that card's <see cref="ICard.WriteExtendedRom"/>.
    /// If <see cref="BankSelect"/> is 0, the write is silently ignored (no floating bus effect).
    /// </para>
    /// <para>
    /// <strong>Special Behavior at $CFFF:</strong><br/>
    /// Writing to $CFFF (0x0FFF) disables extended ROM by setting <see cref="BankSelect"/>
    /// to 0. The write still reaches the currently active card (if any) before being disabled.
    /// </para>
    /// </remarks>
    /// <seealso cref="Read"/>
    /// <seealso cref="BankSelect"/>
    public void Write(ushort address, byte val)
    {
        // $CFFF disables extended ROM (both read and write)
        if (address == 0x0FFF)
        {
            BankSelect = 0;
        }

        // $C090-$C0FF: Card I/O space
        if (address >= 0x0090 && address <= 0x00FF)
        {
            // INTCXROM overrides all card I/O and ROM
            if (_status.StateIntCxRom)
            {
                // Internal ROM enabled, write to system ROM (usually no-op)
                _rom.Write(address, val);
                return;
            }

            // Determine slot from address: $C090-$C09F=slot1, $C0A0-$C0AF=slot2, etc.
            int slot = ((address >> 4) & 0x07);
            byte offset = (byte) (address & 0x0F);

            // Card I/O never changes BankSelect
            _cards[slot].WriteIO(offset, val);
            return;
        }

        // $C100-$C7FF: Card ROM or System ROM writes
        if (address >= 0x0100 && address <= 0x07FF)
        {
            // INTCXROM overrides SLOTCXROM and SLOTC3ROM
            if (_status.StateIntCxRom)
            {
                // Internal ROM enabled for entire $C100-$CFFF range
                _rom.Write(address, val);
                return;
            }

            int slot = (address >> 8) & 0x07;
            byte offset = (byte) (address & 0xFF);

            // Determine if this slot should use card ROM or system ROM
            bool useCardRom = !_status.StateIntCxRom;

            // Special case: Slot 3 is controlled by SLOTC3ROM
            if (slot == 3)
            {
                useCardRom = _status.StateSlotC3Rom;
            }

            if (useCardRom)
            {
                // Card ROM enabled: ACTIVATE extended ROM and write to card
                BankSelect = (byte) slot;
                _cards[slot].WriteRom(offset, val);
            }
            else
            {
                // System ROM enabled: DON'T change BankSelect, write to ROM (usually no-op)
                _rom.Write(address, val);
            }
            return;
        }

        // $C800-$CFFF: Extended ROM writes
        if (address >= 0x0800 && address <= 0x0FFF)
        {
            // INTCXROM overrides extended ROM
            if (_status.StateIntCxRom)
            {
                _rom.Write(address, val);
                return;
            }

            ushort offset = (ushort) (address - 0x0800);

            if (BankSelect != 0)
            {
                _cards[BankSelect].WriteExtendedRom(offset, val);
            }
            return;
        }

        // Shouldn't reach here in normal operation ($C000-$C08F handled elsewhere)
        throw new InvalidOperationException($"ISlots.Write() called with invalid address: ${address + 0xC000:X4}");
    }

    /// <summary>
    /// Gets or sets a byte at the specified address using array-style indexing.
    /// </summary>
    /// <param name="address">
    /// The address to access, offset by $C000. For example, to access $C600, use 0x0600.
    /// </param>
    /// <value>
    /// The byte value at the specified address (reading or writing).
    /// </value>
    /// <remarks>
    /// This indexer provides convenient array-style access to the slots address space,
    /// delegating to <see cref="Read"/> for gets and <see cref="Write"/> for sets.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Read from $C600
    /// byte bootByte = slots[0x0600];
    /// 
    /// // Write to $C0EC (slot 6, I/O offset $0C)
    /// slots[0x00EC] = 0x00;
    /// </code>
    /// </example>
    public byte this[ushort address]
    {
        get => Read(address);
        set => Write(address, value);
    }

    /// <summary>
    /// Gets the current slot configuration as JSON metadata.
    /// </summary>
    /// <returns>
    /// A JSON string containing the configuration of all installed cards and their metadata,
    /// or an empty string if serialization fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method captures the current state of all expansion slots, including which cards
    /// are installed and their individual configurations. The metadata uses JSON format with
    /// the following structure:
    /// </para>
    /// <code>
    /// {
    ///   "version": 1,
    ///   "bankSelect": 6,
    ///   "slots": [
    ///     {
    ///       "slotNumber": 6,
    ///       "cardId": 1,
    ///       "cardName": "Disk II Controller",
    ///       "metadata": "{...card-specific configuration...}"
    ///     }
    ///   ]
    /// }
    /// </code>
    /// <para>
    /// <strong>Hierarchical Configuration:</strong><br/>
    /// The slots system uses the inclusive approach to hierarchical configuration. Each
    /// installed card's metadata (obtained via <see cref="IConfigurable.GetMetadata"/>) is embedded
    /// within the slot configuration. This keeps the entire peripheral configuration
    /// self-contained and portable.
    /// </para>
    /// <para>
    /// <strong>Empty Slots:</strong><br/>
    /// Empty slots (containing <see cref="NullCard"/>) are omitted from the metadata to
    /// keep it concise. On restoration, any slot not mentioned in the metadata is left empty.
    /// </para>
    /// <para>
    /// <strong>BankSelect State:</strong><br/>
    /// The current <see cref="BankSelect"/> value is included in the metadata. While not
    /// strictly necessary for configuration (it's runtime state), including it allows for
    /// precise state restoration during debugging or save states.
    /// </para>
    /// </remarks>
    public string GetMetadata()
    {
        try
        {
            var config = new
            {
                version = 1,
                bankSelect = BankSelect,
                slots = Enumerable.Range(1, 7)
                    .Select(i => (SlotNumber)(i - 1))
                    .Where(slot => !IsEmpty(slot))
                    .Select(slot =>
                    {
                        var card = GetCardIn(slot);
                        return new
                        {
                            slotNumber = (int)slot + 1,
                            cardId = card.Id,
                            cardName = card.Name,
                            metadata = card.GetMetadata()
                        };
                    })
                    .ToArray()
            };

            return System.Text.Json.JsonSerializer.Serialize(config, s_jsonOptions);
        }
        catch
        {
            // Fail-safe: return empty string on any serialization error
            return string.Empty;
        }
    }

    /// <summary>
    /// Applies slot configuration from JSON metadata, restoring installed cards and their configurations.
    /// </summary>
    /// <param name="metadata">
    /// A JSON metadata string previously obtained from <see cref="GetMetadata"/>, or an empty
    /// string to clear all slots.
    /// </param>
    /// <returns>
    /// <c>true</c> if the configuration was successfully applied; <c>false</c> if the metadata
    /// was invalid or any card failed to apply its configuration.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method restores the slot configuration from metadata by:
    /// </para>
    /// <list type="number">
    /// <item><description>Clearing all slots (removing all cards)</description></item>
    /// <item><description>Parsing the JSON metadata</description></item>
    /// <item><description>Installing each card by ID in its designated slot</description></item>
    /// <item><description>Recursively applying each card's embedded metadata</description></item>
    /// <item><description>Restoring the <see cref="BankSelect"/> state</description></item>
    /// </list>
    /// <para>
    /// <strong>Empty String Handling:</strong><br/>
    /// Passing an empty or whitespace-only string clears all slots, leaving the system
    /// with empty slots (all <see cref="NullCard"/> instances). This represents the
    /// default/unconfigured state.
    /// </para>
    /// <para>
    /// <strong>Error Handling:</strong><br/>
    /// If any card fails to install or configure, this method returns <c>false</c>. The
    /// slot system may be left in a partial state where some cards are installed and
    /// others are not. For transactional behavior, callers should capture the current
    /// metadata before applying new metadata to enable rollback.
    /// </para>
    /// <para>
    /// <strong>Card Factory Dependency:</strong><br/>
    /// This method relies on <see cref="ICardFactory.GetCardWithId"/> to create card
    /// instances. If a card ID in the metadata is not registered in the factory, that
    /// slot will remain empty and the method will return <c>false</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Save current configuration
    /// string backup = slots.GetMetadata();
    /// 
    /// // Load a different configuration
    /// if (!slots.ApplyMetadata(savedConfiguration))
    /// {
    ///     // Restore backup on failure
    ///     slots.ApplyMetadata(backup);
    /// }
    /// 
    /// // Clear all slots
    /// slots.ApplyMetadata(string.Empty);
    /// </code>
    /// </example>
    public bool ApplyMetadata(string metadata)
    {
        // Empty string = clear all slots
        if (string.IsNullOrWhiteSpace(metadata))
        {
            for (int i = 1; i <= 7; i++)
            {
                RemoveCard((SlotNumber)(i - 1));
            }
            BankSelect = 0;
            return true;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(metadata);
            var root = doc.RootElement;

            // Clear all slots first
            for (int i = 1; i <= 7; i++)
            {
                RemoveCard((SlotNumber)(i - 1));
            }

            // Track overall success
            bool allSucceeded = true;

            // Restore each slot
            if (root.TryGetProperty("slots", out var slotsArray))
            {
                foreach (var slotConfig in slotsArray.EnumerateArray())
                {
                    if (!slotConfig.TryGetProperty("slotNumber", out var slotNumElement) ||
                        !slotConfig.TryGetProperty("cardId", out var cardIdElement))
                    {
                        allSucceeded = false;
                        continue;
                    }

                    int slotNumber = slotNumElement.GetInt32();
                    int cardId = cardIdElement.GetInt32();

                    if (slotNumber < 1 || slotNumber > 7)
                    {
                        allSucceeded = false;
                        continue;
                    }

                    var slot = (SlotNumber)(slotNumber - 1);

                    try
                    {
                        // Install the card by ID
                        InstallCard(cardId, slot);

                        // Apply card-specific metadata if present
                        if (slotConfig.TryGetProperty("metadata", out var metadataElement))
                        {
                            string cardMetadata = metadataElement.GetString() ?? string.Empty;
                            var card = GetCardIn(slot);

                            if (!card.ApplyMetadata(cardMetadata))
                            {
                                allSucceeded = false;
                            }
                        }
                    }
                    catch
                    {
                        allSucceeded = false;
                    }
                }
            }

            // Restore BankSelect state
            if (root.TryGetProperty("bankSelect", out var bankSelectElement))
            {
                BankSelect = (byte)bankSelectElement.GetInt32();
            }
            else
            {
                BankSelect = 0;
            }

            return allSucceeded;
        }
        catch
        {
            // JSON parsing or other error - return false
            return false;
        }
    }
}
