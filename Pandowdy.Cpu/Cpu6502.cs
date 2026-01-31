// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.Cpu;

/// <summary>
/// NMOS 6502 CPU with undocumented/illegal opcodes.
/// </summary>
public sealed class Cpu6502(CpuStateBuffer buffer) : CpuBase(buffer)
{
    /// <inheritdoc />
    public override CpuVariant Variant => CpuVariant.Nmos6502;

    /// <inheritdoc />
    protected override bool ClearDecimalOnInterrupt => false;
}
