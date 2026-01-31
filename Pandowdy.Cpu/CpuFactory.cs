// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.Cpu;

/// <summary>
/// Factory for creating CPU instances by variant.
/// </summary>
public static class CpuFactory
{
    /// <summary>
    /// Creates a CPU instance for the specified variant.
    /// </summary>
    /// <param name="variant">The CPU variant to emulate.</param>
    /// <param name="buffer">The state buffer to use.</param>
    /// <returns>A CPU instance for the specified variant.</returns>
    public static IPandowdyCpu Create(CpuVariant variant, CpuStateBuffer buffer)
    {
        return variant switch
        {
            CpuVariant.Nmos6502 => new Cpu6502(buffer),
            CpuVariant.Nmos6502Simple => new Cpu6502Simple(buffer),
            CpuVariant.Wdc65C02 => new Cpu65C02(buffer),
            CpuVariant.Rockwell65C02 => new Cpu65C02Rockwell(buffer),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported CPU variant.")
        };
    }
}
