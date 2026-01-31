// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.Cpu;

/// <summary>
/// Specifies the CPU variant to emulate, determining opcode behavior and available instructions.
/// </summary>
/// <remarks>
/// <para>
/// Different 6502 variants have subtle behavioral differences:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Nmos6502"/>: Original MOS 6502 with all undocumented opcodes.</description></item>
///   <item><description><see cref="Nmos6502Simple"/>: MOS 6502 with illegal opcodes treated as NOPs.</description></item>
///   <item><description><see cref="Wdc65C02"/>: Western Design Center 65C02 with extended instructions including bit manipulation.</description></item>
///   <item><description><see cref="Rockwell65C02"/>: Rockwell 65C02, same as WDC but WAI/STP are NOPs.</description></item>
/// </list>
/// </remarks>
public enum CpuVariant
{
    /// <summary>
    /// Original NMOS 6502 with full support for undocumented/illegal opcodes.
    /// Includes JAM instructions that halt the CPU.
    /// </summary>
    Nmos6502,

    /// <summary>
    /// NMOS 6502 variant where illegal opcodes are treated as NOPs instead of
    /// executing their undocumented behavior. Useful for testing or when illegal
    /// opcode behavior is not desired.
    /// </summary>
    Nmos6502Simple,

    /// <summary>
    /// Western Design Center 65C02 (W65C02S). Includes additional instructions
    /// (PHX, PHY, PLX, PLY, STZ, TRB, TSB, BRA, WAI, STP) and bit manipulation
    /// instructions (RMB0-7, SMB0-7, BBR0-7, BBS0-7). Fixes NMOS bugs
    /// (JMP indirect page boundary) and clears decimal mode on interrupt.
    /// </summary>
    Wdc65C02,

    /// <summary>
    /// Rockwell 65C02 variant. Same as WDC65C02 but WAI ($CB) and STP ($DB)
    /// are treated as NOPs instead of halt instructions.
    /// </summary>
    Rockwell65C02
}
