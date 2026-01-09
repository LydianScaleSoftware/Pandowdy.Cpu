using System.Runtime.CompilerServices;

namespace Pandowdy.EmuCore.DataTypes;

/// <summary>
/// Represents a single Apple IIe soft switch with a name and boolean state.
/// </summary>
/// <remarks>
/// <para>
/// Apple IIe soft switches are memory-mapped I/O addresses ($C000-$C0FF) that control
/// hardware behavior. Examples: 80STORE ($C000/$C001), RAMRD ($C002/$C003),
/// TEXT ($C050/$C051), PAGE2 ($C054/$C055).
/// </para>
/// <para>
/// Inherits change-counting functionality to track how often each switch changes state,
/// useful for debugging and performance analysis.
/// </para>
/// </remarks>
public sealed class SoftSwitch(string name, bool initialValue = false)
{
    /// <summary>
    /// Gets the human-readable name of this soft switch (e.g., "80STORE", "RAMRD", "TEXT").
    /// </summary>
    /// <value>The switch name, typically matching Apple IIe documentation conventions.</value>
    public string Name { get; private set; } = name;

    /// <summary>
    /// Gets or sets the current boolean state of the soft switch.
    /// </summary>
    /// <value>True if the switch is on (enabled); false if off (disabled).</value>
    public bool Value { get; set; } = initialValue;

    /// <summary>
    /// Sets the soft switch to the specified value.
    /// </summary>
    /// <param name="val">The new state for the switch (default: true).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(bool val = true)
    {
        Value = val;
    }

    /// <summary>
    /// Gets the current state of the soft switch.
    /// </summary>
    /// <returns>True if the switch is on; false if off.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get()
    {
        return Value;
    }

    /// <summary>
    /// Toggles the soft switch state (on→off, off→on).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Toggle()
    {
        Value = !Value;
    }

    /// <summary>
    /// Returns a string representation of the soft switch showing its name and current state.
    /// </summary>
    /// <returns>A string in the format "SwitchName: True/False".</returns>
    public override string ToString() => $"{Name}: {Value}";
}
