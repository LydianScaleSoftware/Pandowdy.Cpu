namespace Pandowdy.EmuCore.Interfaces;

/// <summary>
/// Callback interface for components that need to respond to Apple II soft switch state changes.
/// </summary>
/// <remarks>
/// This interface is implemented by components that need to react to soft switch changes,
/// such as the system status provider, memory manager, or video renderer. The soft switch
/// manager calls these methods whenever a soft switch is toggled by CPU access to specific
/// I/O addresses ($C000-$C0FF range).
/// <para>
/// Apple II soft switches control various hardware features including:
/// <list type="bullet">
/// <item>Memory banking (main/auxiliary RAM, language card, ROM selection)</item>
/// <item>Video modes (text/graphics, mixed mode, page selection, 80-column)</item>
/// <item>Annunciators (general-purpose output signals)</item>
/// </list>
/// </para>
/// <para>
/// Implementations should update their internal state to reflect the new switch settings
/// and adjust their behavior accordingly. Multiple responders can be registered to receive
/// the same notifications, allowing different subsystems to react independently.
/// </para>
/// </remarks>
public interface ISoftSwitchResponder
{
    // Memory configuration switches
    
    /// <summary>
    /// Called when the 80STORE soft switch is changed ($C000/$C001).
    /// </summary>
    /// <param name="store80">True if 80STORE is enabled; false if disabled</param>
    /// <remarks>
    /// When enabled, page 2 memory ($0800-$0BFF for text, $2000-$3FFF for hi-res)
    /// is redirected to auxiliary memory when PAGE2 is set. This is used for 80-column
    /// text mode and double hi-res graphics to provide independent main and aux video pages.
    /// </remarks>
    void Set80Store(bool store80);
    
    /// <summary>
    /// Called when the RAMRD soft switch is changed ($C002/$C003).
    /// </summary>
    /// <param name="ramRd">True to read from auxiliary memory; false to read from main memory</param>
    /// <remarks>
    /// Controls which 64KB bank (main or auxiliary) is read from for memory accesses.
    /// This affects most memory regions except those controlled by other soft switches
    /// (like 80STORE for video pages). Used for accessing the extended 128KB memory space.
    /// </remarks>
    void SetRamRd(bool ramRd);
    
    /// <summary>
    /// Called when the RAMWRT soft switch is changed ($C004/$C005).
    /// </summary>
    /// <param name="ramWrt">True to write to auxiliary memory; false to write to main memory</param>
    /// <remarks>
    /// Controls which 64KB bank (main or auxiliary) is written to for memory accesses.
    /// Independent from RAMRD, allowing asymmetric configurations (e.g., read from main,
    /// write to aux). Used for accessing the extended 128KB memory space.
    /// </remarks>
    void SetRamWrt(bool ramWrt);
    
    /// <summary>
    /// Called when the INTCXROM soft switch is changed ($C006/$C007).
    /// </summary>
    /// <param name="intCxRom">True to use internal ROM; false to use slot ROMs</param>
    /// <remarks>
    /// Controls whether the $C100-$CFFF range accesses internal ROM or peripheral card
    /// slot ROMs. When true (internal), the built-in ROM is used. When false, individual
    /// slot ROMs can be accessed. Typically set by the monitor during initialization.
    /// </remarks>
    void SetIntCxRom(bool intCxRom);
    
    /// <summary>
    /// Called when the ALTZP soft switch is changed ($C008/$C009).
    /// </summary>
    /// <param name="altZp">True to use auxiliary zero page/stack; false for main</param>
    /// <remarks>
    /// Controls whether zero page ($0000-$00FF) and stack ($0100-$01FF) access main
    /// or auxiliary memory. When true, these critical CPU regions use auxiliary memory,
    /// allowing programs to maintain separate zero page and stack contexts.
    /// </remarks>
    void SetAltZp(bool altZp);
    
    /// <summary>
    /// Called when the SLOTC3ROM soft switch is changed ($C00A/$C00B).
    /// </summary>
    /// <param name="slotC3Rom">True to use slot C3 ROM; false to use internal ROM</param>
    /// <remarks>
    /// Controls whether the $C300-$C3FF range (slot 3) uses the peripheral card ROM
    /// or internal ROM. Independent of INTCXROM, allowing slot 3 to be controlled
    /// separately from other slots. Commonly used for 80-column card firmware.
    /// </remarks>
    void SetSlotC3Rom(bool slotC3Rom);
    
    // Video switches
    
    /// <summary>
    /// Called when the 80VID soft switch is changed ($C00C/$C00D).
    /// </summary>
    /// <param name="vid">True for 80-column mode; false for 40-column mode</param>
    /// <remarks>
    /// Enables 80-column text mode, which uses both main and auxiliary memory to display
    /// 80 characters per line (alternating bytes from main and aux memory). When false,
    /// standard 40-column text mode is used.
    /// </remarks>
    void Set80Vid(bool vid);
    
    /// <summary>
    /// Called when the ALTCHAR soft switch is changed ($C00E/$C00F).
    /// </summary>
    /// <param name="altChar">True for MouseText character set; false for standard</param>
    /// <remarks>
    /// Selects between the standard Apple II character set and the alternate MouseText
    /// character set. MouseText provides graphical symbols for building text-based UIs
    /// (boxes, arrows, icons) in the $40-$5F character range.
    /// </remarks>
    void SetAltChar(bool altChar);
    
    /// <summary>
    /// Called when the TEXT soft switch is changed ($C050/$C051).
    /// </summary>
    /// <param name="text">True for text mode; false for graphics mode</param>
    /// <remarks>
    /// Controls the primary video mode. When true, displays 40-column or 80-column text.
    /// When false, displays lo-res (40×48 color blocks) or hi-res (280×192 pixels)
    /// graphics, depending on the HIRES switch.
    /// </remarks>
    void SetText(bool text);
    
    /// <summary>
    /// Called when the MIXED soft switch is changed ($C052/$C053).
    /// </summary>
    /// <param name="mixed">True for mixed mode; false for full-screen mode</param>
    /// <remarks>
    /// When true and in graphics mode, displays graphics on the top 20 rows (160 scanlines)
    /// and text on the bottom 4 rows (32 scanlines). Commonly used in games to show
    /// graphics with a text status line. Has no effect in text mode.
    /// </remarks>
    void SetMixed(bool mixed);
    
    /// <summary>
    /// Called when the PAGE2 soft switch is changed ($C054/$C055).
    /// </summary>
    /// <param name="page2">True for page 2; false for page 1</param>
    /// <remarks>
    /// Selects which video page is displayed. Page 1 uses $0400-$07FF (text) or
    /// $2000-$3FFF (hi-res). Page 2 uses $0800-$0BFF (text) or $4000-$5FFF (hi-res).
    /// Can be used for page flipping animation. Interaction with 80STORE affects
    /// whether page 2 uses main or auxiliary memory.
    /// </remarks>
    void SetPage2(bool page2);
    
    /// <summary>
    /// Called when the HIRES soft switch is changed ($C056/$C057).
    /// </summary>
    /// <param name="hires">True for hi-res graphics; false for lo-res graphics</param>
    /// <remarks>
    /// In graphics mode (TEXT off), controls whether to display hi-res graphics
    /// (280×192 pixels, 6 colors) or lo-res graphics (40×48 color blocks, 16 colors).
    /// Has no effect when TEXT mode is enabled.
    /// </remarks>
    void SetHiRes(bool hires);
    
    // Annunciators
    
    /// <summary>
    /// Called when annunciator 0 is changed ($C058/$C059).
    /// </summary>
    /// <param name="an0">True if annunciator 0 is on; false if off</param>
    /// <remarks>
    /// Annunciator 0 is a general-purpose output signal. Originally intended for
    /// controlling external devices, rarely used in standard software. Some games
    /// use annunciators for audio effects or peripheral control.
    /// </remarks>
    void SetAn0(bool an0);
    
    /// <summary>
    /// Called when annunciator 1 is changed ($C05A/$C05B).
    /// </summary>
    /// <param name="an1">True if annunciator 1 is on; false if off</param>
    /// <remarks>
    /// Annunciator 1 is a general-purpose output signal. Originally intended for
    /// controlling external devices, rarely used in standard software.
    /// </remarks>
    void SetAn1(bool an1);
    
    /// <summary>
    /// Called when annunciator 2 is changed ($C05C/$C05D).
    /// </summary>
    /// <param name="an2">True if annunciator 2 is on; false if off</param>
    /// <remarks>
    /// Annunciator 2 is a general-purpose output signal. Originally intended for
    /// controlling external devices, rarely used in standard software.
    /// </remarks>
    void SetAn2(bool an2);
    
    /// <summary>
    /// Called when annunciator 3 is changed ($C05E/$C05F).
    /// </summary>
    /// <param name="an3">True if annunciator 3 is on; false if off</param>
    /// <remarks>
    /// Annunciator 3 doubles as the double hi-res (DHR) enable switch. 
    /// <para>
    /// <strong>Important:</strong> The logic is inverted - when this annunciator is 
    /// <em>off</em> (false), double hi-res mode is <em>enabled</em>. When the annunciator 
    /// is <em>on</em> (true), double hi-res is <em>disabled</em>. This backwards behavior 
    /// is a quirk of the Apple IIe hardware design.
    /// </para>
    /// <para>
    /// Double hi-res mode (560×192 pixels, 16 colors) uses both main and auxiliary video 
    /// pages. When not used for DHGR control, this functions as a general-purpose output 
    /// signal like the other annunciators.
    /// </para>
    /// </remarks>
    void SetAn3(bool an3);

    // Language card switches
    
    /// <summary>
    /// Called when the language card bank selection changes ($C080-$C08F).
    /// </summary>
    /// <param name="enabled">True for bank 1 (D000-DFFF physical at C000); false for bank 2 (at D000)</param>
    /// <remarks>
    /// The language card provides two 4KB RAM banks that can be mapped into the
    /// $D000-$DFFF address space. Bank 1 is physically located at $C000-$CFFF,
    /// while bank 2 is at $D000-$DFFF. This is often confusing because bank 1's
    /// physical location overlaps with I/O space in the address map.
    /// </remarks>
    void SetBank1(bool enabled);
    
    /// <summary>
    /// Called when the language card write enable state changes ($C080-$C08F).
    /// </summary>
    /// <param name="enabled">True if writing to language card RAM is enabled; false if write-protected</param>
    /// <remarks>
    /// Controls whether writes to the $D000-$FFFF range modify the language card RAM
    /// or are ignored. Write protection is engaged through a two-access sequence to
    /// prevent accidental modification of critical code. When false, writes to this
    /// region have no effect (ROM remains visible during reads).
    /// </remarks>
    void SetHighWrite(bool enabled);
    
    /// <summary>
    /// Called when the language card read enable state changes ($C080-$C08F).
    /// </summary>
    /// <param name="enabled">True to read from language card RAM; false to read from ROM</param>
    /// <remarks>
    /// Controls whether reads from $D000-$FFFF access the language card RAM or the
    /// built-in ROM. When true, the selected bank ($D000-$DFFF) and upper 8KB
    /// ($E000-$FFFF) are read from RAM. When false, ROM is visible instead.
    /// Independent of write enable, allowing read-from-ROM, write-to-RAM configurations.
    /// </remarks>
    void SetHighRead(bool enabled);
    
    /// <summary>
    /// Called when the language card pre-write state changes ($C080-$C08F).
    /// </summary>
    /// <param name="enabled">True if pre-write sequence is active; false otherwise</param>
    /// <remarks>
    /// The language card uses a two-access sequence to enable writing, preventing
    /// accidental writes. The first access sets pre-write (this method), and the
    /// second consecutive access enables writing (SetHighWrite). Accessing different
    /// language card addresses resets the sequence.
    /// </remarks>
    void SetPreWrite(bool enabled);
    
    // Pushbuttons (game controller)
    
    /// <summary>
    /// Called when pushbutton 0 state changes (readable at $C061).
    /// </summary>
    /// <param name="pressed">True if button is pressed; false if released</param>
    /// <remarks>
    /// Pushbutton 0 is typically mapped to the Open-Apple key (Command on modern keyboards).
    /// Returns bit 7 set when pressed. Used by games and applications for user input.
    /// </remarks>
    void SetButton0(bool pressed);
    
    /// <summary>
    /// Called when pushbutton 1 state changes (readable at $C062).
    /// </summary>
    /// <param name="pressed">True if button is pressed; false if released</param>
    /// <remarks>
    /// Pushbutton 1 is typically mapped to the Closed-Apple/Solid-Apple key (Option on modern keyboards).
    /// Returns bit 7 set when pressed. Used by games and applications for user input.
    /// </remarks>
    void SetButton1(bool pressed);
    
    /// <summary>
    /// Called when pushbutton 2 state changes (readable at $C063).
    /// </summary>
    /// <param name="pressed">True if button is pressed; false if released</param>
    /// <remarks>
    /// Pushbutton 2 is typically mapped to the Shift key for joystick button emulation.
    /// Returns bit 7 set when pressed. Used by games and applications for user input.
    /// </remarks>
    void SetButton2(bool pressed);
}
