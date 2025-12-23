using Emulator;

namespace Pandowdy.EmuCore.Interfaces;

/// <summary>
/// Apple II specific bus interface that extends the base IBus with
/// keyboard input and pushbutton (game controller) functionality.
/// </summary>
public interface IAppleIIBus : IBus
{
    /// <summary>
    /// Sets the keyboard latch value (typically with high bit set).
    /// </summary>
    void SetKeyValue(byte key);

    /// <summary>
    /// Sets the state of a pushbutton (game controller button).
    /// </summary>
    /// <param name="num">Button number (0-2)</param>
    /// <param name="enabled">True if pressed, false if released</param>
    void SetPushButton(int num, bool enabled);
}
