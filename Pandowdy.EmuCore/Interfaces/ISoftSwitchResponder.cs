namespace Pandowdy.EmuCore.Interfaces;

/// <summary>
/// Responder interface for Apple II soft switch changes.
/// Implementations react to soft switch state changes (memory banking, video modes, etc.).
/// Called by the soft switch manager when switches are toggled.
/// </summary>
public interface ISoftSwitchResponder
{
    // Memory configuration switches
    void Set80Store(bool store80);
    void SetRamRd(bool ramRd);
    void SetRamWrt(bool ramWrt);
    void SetIntCxRom(bool intCxRom);
    void SetAltZp(bool altZp);
    void SetSlotC3Rom(bool slotC3Rom);
    
    // Video switches
    void Set80Vid(bool vid);
    void SetAltChar(bool altChar);
    void SetText(bool text);
    void SetMixed(bool mixed);
    void SetPage2(bool page2);
    void SetHiRes(bool hires);
    
    // Annunciators
    void SetAn0(bool an0);
    void SetAn1(bool an1);
    void SetAn2(bool an2);
    void SetAn3(bool an3);

    // Language card switches
    void SetBank1(bool enabled);
    void SetHighWrite(bool enabled);
    void SetHighRead(bool enabled);
    void SetPreWrite(bool enabled);
}
